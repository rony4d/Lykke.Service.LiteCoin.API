﻿using Autofac;
using Common.Log;
using Lykke.Service.BlockchainSignService.Client;
using Lykke.Service.LiteCoin.API.Core.Address;
using Lykke.Service.LiteCoin.API.Core.BlockChainReaders;
using Lykke.Service.LiteCoin.API.Core.Broadcast;
using Lykke.Service.LiteCoin.API.Core.CashIn;
using Lykke.Service.LiteCoin.API.Core.CashOut;
using Lykke.Service.LiteCoin.API.Core.Fee;
using Lykke.Service.LiteCoin.API.Core.Operation;
using Lykke.Service.LiteCoin.API.Core.Settings.ServiceSettings;
using Lykke.Service.LiteCoin.API.Core.Sign;
using Lykke.Service.LiteCoin.API.Core.TransactionOutputs;
using Lykke.Service.LiteCoin.API.Core.TransactionOutputs.BroadcastedOutputs;
using Lykke.Service.LiteCoin.API.Core.TransactionOutputs.SpentOutputs;
using Lykke.Service.LiteCoin.API.Core.Transactions;
using Lykke.Service.LiteCoin.API.Core.Wallet;
using Lykke.Service.LiteCoin.API.Services.Address;
using Lykke.Service.LiteCoin.API.Services.BlockChainProviders.InsightApi;
using Lykke.Service.LiteCoin.API.Services.Broadcast;
using Lykke.Service.LiteCoin.API.Services.Fee;
using Lykke.Service.LiteCoin.API.Services.Operations;
using Lykke.Service.LiteCoin.API.Services.Operations.CashIn;
using Lykke.Service.LiteCoin.API.Services.Operations.CashOut;
using Lykke.Service.LiteCoin.API.Services.Sign;
using Lykke.Service.LiteCoin.API.Services.SourceWallet;
using Lykke.Service.LiteCoin.API.Services.TransactionOutputs;
using Lykke.Service.LiteCoin.API.Services.TransactionOutputs.BroadcastedOutputs;
using Lykke.Service.LiteCoin.API.Services.TransactionOutputs.SpentOutputs;
using Lykke.Service.LiteCoin.API.Services.Transactions;
using Lykke.Service.LiteCoin.API.Services.Wallet;
using Lykke.SettingsReader;
using NBitcoin;

namespace Lykke.Service.LiteCoin.API.Services.Binder
{
    public  class ServiceModule:Module
    {
        private readonly ILog _log;
        private readonly IReloadingManager<LiteCoinAPISettings> _settings;
        public ServiceModule(IReloadingManager<LiteCoinAPISettings> settings, ILog log)
        {
            _log = log;
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            RegisterNetwork(builder);
            RegisterFeeServices(builder);
            RegisterAddressValidatorServices(builder);
            RegisterInsightApiBlockChainReaders(builder);
            RegisterSignFacadeServices(builder);
            RegisterDetectorServices(builder);
            RegisterTransactionOutputsServices(builder);
            RegisterTransactionBuilderServices(builder);
            RegisterBroadcastServices(builder);
        }

        private void RegisterNetwork(ContainerBuilder builder)
        {
            NBitcoin.Litecoin.Networks.Register();

            builder.RegisterInstance(Network.GetNetwork(_settings.CurrentValue.Network)).As<Network>();
        }

        private void RegisterFeeServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(new FeeRateFacade(_settings.CurrentValue.FeePerByte))
                .As<IFeeRateFacade>();

            builder.Register(x =>
            {
                var resolver = x.Resolve<IComponentContext>();
                return new FeeService(resolver.Resolve<IFeeRateFacade>(), 
                    _settings.CurrentValue.MinFeeValue, 
                    _settings.CurrentValue.MaxFeeValue);
            }).As<IFeeService>();
        }

        private void RegisterAddressValidatorServices(ContainerBuilder builder)
        {
            builder.RegisterType<AddressValidator>().As<IAddressValidator>();
        }

        private void RegisterInsightApiBlockChainReaders(ContainerBuilder builder)
        {
            builder.RegisterInstance(new InsightApiSettings
            {
                Url = _settings.CurrentValue.InsightAPIUrl
            }).SingleInstance();

            builder.RegisterType<InsightApiBlockChainProvider>().As<IBlockChainProvider>();
        }
        
        private void RegisterSignFacadeServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(new SignSettings
            {
                Url = _settings.CurrentValue.SignFacadeUrl
            }).SingleInstance();

            builder.RegisterInstance(new BlockchainSignServiceClient(_settings.CurrentValue.SignFacadeUrl, _log)).AsSelf();
            builder.RegisterType<SignService>().As<ISignService>().SingleInstance();
            builder.RegisterType<BlockchainSignServiceApiProvider>().As<IBlockchainSignServiceApiProvider>().SingleInstance();

            RegisterWalletServices(builder);
        }

        private void RegisterWalletServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(new HotWalletsSettings
            {
                SourceWalletPublicAddresses = _settings.CurrentValue.SourceWallets
            }).SingleInstance();

            builder.RegisterType<WalletService>().As<IWalletService>();
            builder.RegisterType<WalletBalanceService>().As<IWalletBalanceService>();
        }

        private void RegisterDetectorServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(new OperationsConfirmationsSettings
            {
                MinCashInConfirmations = _settings.CurrentValue.MinCashInConfirmationsCount,
                MinCashOutConfirmations = _settings.CurrentValue.MinCashOutConfirmationsCount,
                MinCashInRetryConfirmations = _settings.CurrentValue.MinCashInRetryConfirmationsCount
            });
            
            RegisterCashInDetectorServices(builder);
            RegisterCashOutsDetectorServices(builder);
        }

        private void RegisterCashInDetectorServices(ContainerBuilder builder)
        {
            builder.RegisterType<CashInOperationDetectorFacade>()
                .As<ICashInOperationDetectorFacade>();

            builder.RegisterType<SettledCashInTransactionDetector>()
                .As<ISettledCashInTransactionDetector>();

            builder.RegisterType<SettledCashInTransactionHandler>()
                .As<ISettledCashInTransactionHandler>();
        }

        private void RegisterCashOutsDetectorServices(ContainerBuilder builder)
        {
            builder.RegisterType<SettledCashOutTransactionDetector>()
                .As<ISettledCashOutTransactionDetector>();

            builder.RegisterType<SettledCashoutTransactionHandler>()
                .As<ISettledCashoutTransactionHandler>();

            builder.RegisterType<CashOutsOperationDetectorFacade>()
                .As<ICashOutsOperationDetectorFacade>();
        }

        private void RegisterTransactionOutputsServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(new TransactionOutputsExpirationSettings
            {
                BroadcastedOutputsExpirationDays = _settings.CurrentValue.BroadcastedOutputsExpirationDays,
                SpentOutputsExpirationDays = _settings.CurrentValue.SpentOutputsExpirationDays
            });

            builder.RegisterType<TransactionOutputsService>().As<ITransactionOutputsService>();
            builder.RegisterType<SpentOutputService>().As<ISpentOutputService>();
            builder.RegisterType<BroadcastedOutputsService>().As<IBroadcastedOutputsService>();
        }

        private void RegisterTransactionBuilderServices(ContainerBuilder builder)
        {
            builder.RegisterType<TransactionBuilderService>().As<ITransactionBuilderService>();
            builder.RegisterType<OperationService>().As<IOperationService>();
        }

        private void RegisterBroadcastServices(ContainerBuilder builder)
        {
            builder.RegisterType<BroadcastService>().As<IBroadcastService>();
        }
    }
}
