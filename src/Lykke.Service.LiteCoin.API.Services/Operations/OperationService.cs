﻿using System;
using System.Threading.Tasks;
using Lykke.Service.LiteCoin.API.Core.Exceptions;
using Lykke.Service.LiteCoin.API.Core.Operation;
using Lykke.Service.LiteCoin.API.Core.TransactionOutputs.SpentOutputs;
using Lykke.Service.LiteCoin.API.Core.Transactions;
using NBitcoin;

namespace Lykke.Service.LiteCoin.API.Services.Operations
{
    public class OperationService:IOperationService
    {
        private readonly ITransactionBuilderService _transactionBuilder;
        private readonly IOperationMetaRepository _operationMetaRepository;
        private readonly ITransactionBlobStorage _transactionBlobStorage;

        public OperationService(ITransactionBuilderService transactionBuilder,
            IOperationMetaRepository operationMetaRepository, 
            ISpentOutputService spentOutputService,
            ITransactionBlobStorage transactionBlobStorage)
        {
            _transactionBuilder = transactionBuilder;
            _operationMetaRepository = operationMetaRepository;
            _transactionBlobStorage = transactionBlobStorage;
        }

        public async Task<Transaction> BuildTransferTransaction(Guid operationId,
            BitcoinAddress fromAddress, 
            BitcoinAddress toAddress,
            string assetId,
            Money amountToSend, bool includeFee)
        {
            if (await _operationMetaRepository.Exist(operationId))
            {
                var txData = await _transactionBlobStorage.GetTransaction(operationId, TransactionBlobType.Initial);

                return Transaction.Parse(txData);
            }
            
            var buildedTransaction = await _transactionBuilder.GetTransferTransaction(fromAddress, toAddress, amountToSend, includeFee);

            await _transactionBlobStorage.AddOrReplaceTransaction(operationId, TransactionBlobType.Initial,
                buildedTransaction.TransactionData.ToHex());

            var operation = OperationMeta.Create(operationId, fromAddress.ToString(), toAddress.ToString(), assetId,
                buildedTransaction.Amount.Satoshi, buildedTransaction.Fee.Satoshi, includeFee);
            await _operationMetaRepository.Insert(operation);

            return buildedTransaction.TransactionData;
        }
    }
}
