﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.LiteCoin.API.Core.BlockChainReaders;
using Lykke.Service.LiteCoin.API.Core.Pagination;
using Lykke.Service.LiteCoin.API.Core.Wallet;
using Lykke.Service.LiteCoin.API.Services.Operations;
using NBitcoin;

namespace Lykke.Service.LiteCoin.API.Services.Wallet
{
    public class WalletBalanceService:IWalletBalanceService
    {
        private readonly IWalletBalanceRepository _balanceRepository;
        private readonly IObservableWalletRepository _observableWalletRepository;
        private readonly IBlockChainProvider _blockChainProvider;
        private readonly OperationsConfirmationsSettings _confirmationsSettings;

        public WalletBalanceService(IWalletBalanceRepository balanceRepository, 
            IObservableWalletRepository observableWalletRepository,
            IBlockChainProvider blockChainProvider, 
            OperationsConfirmationsSettings confirmationsSettings)
        {
            _balanceRepository = balanceRepository;
            _observableWalletRepository = observableWalletRepository;
            _blockChainProvider = blockChainProvider;
            _confirmationsSettings = confirmationsSettings;
        }

        public async Task Subscribe(string address)
        {
            await _observableWalletRepository.Insert(ObservableWallet.Create(address));
        }

        public async Task Unsubscribe(string address)
        {
            await _observableWalletRepository.Delete(address);
            await _balanceRepository.DeleteIfExist(address);
        }

        public async Task<IPaginationResult<IWalletBalance>> GetBalances(int take, string continuation)
        {
            return await _balanceRepository.GetBalances(take, continuation);
        }

        public async Task<IWalletBalance> UpdateBalance(string address)
        {
            var wallet = await _observableWalletRepository.Get(address);
            if (wallet != null)
            {
                return await UpdateBalance(wallet);
            }

            return null;
        }

        public async Task<IWalletBalance> UpdateBalance(IObservableWallet wallet)
        {
            if (wallet != null)
            {
                var balance = await _blockChainProvider.GetBalanceSatoshiFromUnspentOutputs(wallet.Address, _confirmationsSettings.MinConfirmationsToDetectOperation);
                var lastBlock = await _blockChainProvider.GetLastBlockHeight();

                if (balance != 0)
                {
                    var walletBalanceEntity = WalletBalance.Create(wallet.Address, balance, lastBlock);
                    await _balanceRepository.InsertOrReplace(walletBalanceEntity);

                    return walletBalanceEntity;
                }
                else
                {
                    await _balanceRepository.DeleteIfExist(wallet.Address);
                }
            }

            return null;
        }
    }
}
