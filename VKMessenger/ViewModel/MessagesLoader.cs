﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VKMessenger.Model;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKMessenger.ViewModel
{
    public class MessagesLoader : INotifyPropertyChanged
    {
        private Messenger _messenger;
        public VkApi Vk { get { return _messenger.Vk; } }

        private Messages _model = new Messages();
        public Messages Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    OnPropertyChanged();
                }
            }
        }

        private Dialog _dialog;
        public Dialog Dialog
        {
            get { return _dialog; }
            set
            {
                if (_dialog != value)
                {
                    _dialog = value;
                    OnDialogChanged();
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MessagesLoader(Messenger messenger)
        {
            _messenger = messenger;
        }

        protected virtual async void OnDialogChanged()
        {
            long peerId = Dialog.PeerId;
            MessagesGetObject history = await LoadHistory(peerId);
            Model.SetData(history.Messages);
            Dialog.Messages.Content = Model.Content;
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Task<MessagesGetObject> LoadHistory(long peerId)
        {
            return Task.Run(() =>
            {
                Utils.Extensions.SleepIfTooManyRequests(Vk);

                return Vk.Messages.GetHistory(new MessagesGetHistoryParams()
                {
                    PeerId = peerId,
                    Count = 10
                });
            });
        }
    }
}