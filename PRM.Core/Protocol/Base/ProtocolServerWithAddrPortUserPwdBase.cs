﻿using PRM.Core.Model;
using Shawn.Utils;

namespace PRM.Core.Protocol
{
    public abstract class ProtocolServerWithAddrPortUserPwdBase : ProtocolServerWithAddrPortBase
    {
        protected ProtocolServerWithAddrPortUserPwdBase(string protocol, string classVersion, string protocolDisplayName, string protocolDisplayNameInShort = "") : base(protocol, classVersion, protocolDisplayName, protocolDisplayNameInShort)
        {
        }

        #region Conn

        private string _userName = "Administrator";
        [OtherName(Name = "PRM_USER_NAME")]
        public string UserName
        {
            get => _userName;
            set => SetAndNotifyIfChanged(nameof(UserName), ref _userName, value);
        }

        private string _password = "";
        [OtherName(Name = "PRM_PASSWORD")]
        public string Password
        {
            get => _password;
            set => SetAndNotifyIfChanged(nameof(Password), ref _password, value);
        }

        protected override string GetSubTitle()
        {
            return $"@{Address}:{Port} ({UserName})";
        }

        #endregion
    }
}
