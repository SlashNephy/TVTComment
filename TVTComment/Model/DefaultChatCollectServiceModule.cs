﻿using ObservableUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading;
using System.Linq;
using System.Collections.Specialized;

namespace TVTComment.Model
{
    /// <summary>
    /// ユーザーが設定した既定のコメント元の自動選択に関する処理をする
    /// </summary>
    class DefaultChatCollectServiceModule
    {
        private TVTCommentSettings settings;
        private ChannelInformationModule channelInformationModule;
        private ChatCollectServiceModule collectServiceModule;
        private IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> serviceEntryList;
        private CompositeDisposable disposables=new CompositeDisposable();
        private bool? latestIsRecord = null;

        public ObservableValue<bool> IsEnabled { get; } = new ObservableValue<bool>();
        /// <summary>
        /// リアルタイムで放送中の番組の視聴中にデフォルトで選択する<seealso cref="IChatCollectServiceEntry"/>
        /// <see cref="SynchronizationContext"/>上から操作される
        /// </summary>
        public ObservableCollection<ChatCollectServiceEntry.IChatCollectServiceEntry> LiveChatCollectService { get; } = new ObservableCollection<ChatCollectServiceEntry.IChatCollectServiceEntry>();
        /// <summary>
        /// 録画番組の視聴中にデフォルトで選択する<seealso cref="IChatCollectServiceEntry"/>
        /// <see cref="SynchronizationContext"/>上から操作される
        /// </summary>
        public ObservableCollection<ChatCollectServiceEntry.IChatCollectServiceEntry> RecordChatCollectService { get; } = new ObservableCollection<ChatCollectServiceEntry.IChatCollectServiceEntry>();

        /// <summary>
        /// コンストラクタの引数で指定した<seealso cref="ChannelInformationModule"/>の<seealso cref="ChannelInformationModule.SynchronizationContext"/>と
        /// <seealso cref="ChatCollectServiceModule"/>の<seealso cref="ChatCollectServiceModule.SynchronizationContext"/>と同じ
        /// </summary>
        public SynchronizationContext SynchronizationContext => channelInformationModule.SynchronizationContext;

        public DefaultChatCollectServiceModule(
            TVTCommentSettings settings, ChannelInformationModule channelInformationModule,
            ChatCollectServiceModule collectServiceModule, IEnumerable<ChatCollectServiceEntry.IChatCollectServiceEntry> serviceEntryList
        )
        {
            this.settings = settings;
            this.channelInformationModule = channelInformationModule;
            this.collectServiceModule = collectServiceModule;
            this.serviceEntryList = serviceEntryList;

            loadSettings();
            disposables.Add(channelInformationModule.CurrentTime.Subscribe(timeChanged));
            disposables.Add(LiveChatCollectService.ObserveCollectionChanged(newServiceEntry =>
            {
                if (IsEnabled.Value && !latestIsRecord.GetValueOrDefault(true))
                    collectServiceModule.AddService(newServiceEntry, null);
            }, oldServiceEntry =>
             {
                 if (IsEnabled.Value && !latestIsRecord.GetValueOrDefault(true))
                     foreach (var service in collectServiceModule.RegisteredServices.Where(x => x.ServiceEntry == oldServiceEntry))
                         collectServiceModule.RemoveService(service);
             }, () =>
             {
                 if (IsEnabled.Value && !latestIsRecord.GetValueOrDefault(true))
                     collectServiceModule.ClearServices();
             }));
            disposables.Add(RecordChatCollectService.ObserveCollectionChanged(newServiceEntry =>
            {
                if (IsEnabled.Value && latestIsRecord.GetValueOrDefault(false))
                    collectServiceModule.AddService(newServiceEntry, null);
            }, oldServiceEntry =>
            {
                if (IsEnabled.Value && latestIsRecord.GetValueOrDefault(false))
                    foreach (var service in collectServiceModule.RegisteredServices.Where(x => x.ServiceEntry == oldServiceEntry))
                        collectServiceModule.RemoveService(service);
            }, () =>
            {
                if (IsEnabled.Value && latestIsRecord.GetValueOrDefault(false))
                    collectServiceModule.ClearServices();
            }));

            disposables.Add(channelInformationModule.CurrentTime.Subscribe(timeChanged));
        }

        private void timeChanged(DateTime? time)
        {
            if (!IsEnabled.Value || !time.HasValue)
                return;

            bool isRecord = (getDateTimeJstNow() - time.Value).TotalMinutes > 3;
            if (isRecord==latestIsRecord)
                return;

            latestIsRecord = isRecord;
            if(isRecord)
            {
                //録画
                collectServiceModule.ClearServices();
                foreach (var serviceEntry in RecordChatCollectService)
                    collectServiceModule.AddService(serviceEntry, null);
            }
            else
            {
                //リアルタイム
                collectServiceModule.ClearServices();
                foreach (var serviceEntry in LiveChatCollectService)
                    collectServiceModule.AddService(serviceEntry,null);
            }
        }

        public void Dispose()
        {
            saveSettings();
            disposables.Dispose();
        }

        private void saveSettings()
        {
            this.settings.UseDefaultChatCollectService = this.IsEnabled.Value;

            this.settings.LiveDefaultChatCollectServices = this.LiveChatCollectService.Select(x => x.Id).ToArray();
            this.settings.RecordDefaultChatCollectServices = this.RecordChatCollectService.Select(x => x.Id).ToArray();
        }

        private void loadSettings()
        {
            this.IsEnabled.Value = this.settings.UseDefaultChatCollectService;

            foreach (string id in this.settings.LiveDefaultChatCollectServices)
            {
                var serviceEntry = serviceEntryList.SingleOrDefault(x => x.Id == id);
                if(serviceEntry != null)
                    this.LiveChatCollectService.Add(serviceEntry);
            }
            foreach (string id in this.settings.RecordDefaultChatCollectServices)
            {
                var serviceEntry = serviceEntryList.SingleOrDefault(x => x.Id == id);
                if (serviceEntry != null)
                    this.RecordChatCollectService.Add(serviceEntry);
            }
        }

        private static DateTime getDateTimeJstNow()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9), DateTimeKind.Unspecified);
        }
    }
}
