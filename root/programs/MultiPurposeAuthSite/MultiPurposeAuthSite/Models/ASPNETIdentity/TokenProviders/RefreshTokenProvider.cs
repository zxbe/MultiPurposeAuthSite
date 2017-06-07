﻿//**********************************************************************************
//* Copyright (C) 2007,2016 Hitachi Solutions,Ltd.
//**********************************************************************************

#region Apache License
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

//**********************************************************************************
//* クラス名        ：RefreshTokenProvider
//* クラス日本語名  ：RefreshTokenProvider（ライブラリ）
//*
//* 作成日時        ：－
//* 作成者          ：－
//* 更新履歴        ：－
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2017/04/24  西野 大介         新規
//**********************************************************************************

using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.DataHandler.Serializer;

using Dapper;

using MultiPurposeAuthSite.Models.Util;
using Touryo.Infrastructure.Public.Util;

namespace MultiPurposeAuthSite.Models.ASPNETIdentity.TokenProviders
{
    /// <summary>
    /// RefreshTokenのProvider
    /// SerializeTicket一時保存する。
    /// </summary>
    /// <remarks>c# - OWIN Security - How to Implement OAuth2 Refresh Tokens - Stack Overflow</remarks>
    /// <see cref="http://stackoverflow.com/questions/20637674/owin-security-how-to-implement-oauth2-refresh-tokens"/>
    /// <seealso cref="https://tools.ietf.org/html/rfc6749#section-1.5"/>
    public class RefreshTokenProvider : IAuthenticationTokenProvider
    {
        /// <summary>シングルトン</summary>
        private static RefreshTokenProvider _RefreshTokenProvider = new RefreshTokenProvider();

        /// <summary>
        /// _refreshTokens
        /// ConcurrentDictionaryは、.NET 4.0の新しいスレッドセーフなHashtable
        /// </summary>
        private static ConcurrentDictionary<string, AuthenticationTicket>
            RefreshTokens = new ConcurrentDictionary<string, AuthenticationTicket>();
        
        /// <summary>GetInstance</summary>
        /// <returns>RefreshTokenProvider</returns>
        public static RefreshTokenProvider GetInstance()
        {
            return RefreshTokenProvider._RefreshTokenProvider;
        }

        #region Create

        /// <summary>Create</summary>
        /// <param name="context">AuthenticationTokenCreateContext</param>
        public void Create(AuthenticationTokenCreateContext context)
        {
            this.CreateRefreshToken(context);
        }

        public Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            return Task.Factory.StartNew(() => this.CreateRefreshToken(context));
        }

        /// <summary>CreateRefreshToken</summary>
        /// <param name="context"></param>
        private void CreateRefreshToken(AuthenticationTokenCreateContext context)
        {
            // context.SetToken(context.SerializeTicket());

            // --------------------------------------------------
            // --------------------------------------------------

            string token = GetPassword.Base64UrlSecret(128); // Guid.NewGuid().ToString();

            // copy properties and set the desired lifetime of refresh token.
            AuthenticationProperties refreshTokenProperties = new AuthenticationProperties(context.Ticket.Properties.Dictionary)
            {
                // IssuedUtcとExpiredUtcという有効期限プロパティをAuthenticationTicketに追加
                IssuedUtc = context.Ticket.Properties.IssuedUtc,
                ExpiresUtc = DateTime.UtcNow.Add(ASPNETIdentityConfig.OAuthRefreshTokenExpireTimeSpanFromDays) // System.TimeSpan.FromSeconds(20)) // Debug時  
            };

            // AuthenticationTicket.IdentityのClaimsIdentity値を含む有効期限付きの新しいAuthenticationTicketを作成する。
            AuthenticationTicket refreshTokenTicket = new AuthenticationTicket(context.Ticket.Identity, refreshTokenProperties);

            // 新しいrefreshTokenTicketをConcurrentDictionaryに保存
            // consider storing only the hash of the handle.

            TicketSerializer serializer = new TicketSerializer();
            byte[] bytes = serializer.Serialize(refreshTokenTicket);

            switch (ASPNETIdentityConfig.UserStoreType)
            {
                case EnumUserStoreType.Memory:
                    RefreshTokenProvider.RefreshTokens.TryAdd(token, refreshTokenTicket);
                    break;

                case EnumUserStoreType.SqlServer:
                case EnumUserStoreType.OracleMD:
                case EnumUserStoreType.PostgreSQL: // DMBMS

                    using (IDbConnection cnn = DataAccess.CreateConnection())
                    {
                        cnn.Open();

                        switch (ASPNETIdentityConfig.UserStoreType)
                        {
                            case EnumUserStoreType.SqlServer:

                                cnn.Execute(
                                    "INSERT INTO [RefreshTokenDictionary] ([Key], [Value]) VALUES (@Key, @Value)",
                                    new { Key = token, Value = bytes });

                                break;

                            case EnumUserStoreType.OracleMD:

                                cnn.Execute(
                                    "INSERT INTO \"RefreshTokenDictionary\" (\"Key\", \"Value\") VALUES (:Key, :Value)",
                                    new { Key = token, Value = bytes });

                                break;

                            case EnumUserStoreType.PostgreSQL:

                                break;

                        }
                    }

                    break;
            }

            context.SetToken(token);
        }

        #endregion

        #region Receive

        /// <summary>Receive</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        public void Receive(AuthenticationTokenReceiveContext context)
        {
            this.ReceiveRefreshToken(context);
        }

        /// <summary>ReceiveAsync</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        /// <returns>Task</returns>
        public Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            return Task.Factory.StartNew(() => this.ReceiveRefreshToken(context));
        }

        /// <summary>ReceiveRefreshToken</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        private void ReceiveRefreshToken(AuthenticationTokenReceiveContext context)
        {
            //context.DeserializeTicket(context.Token);

            // --------------------------------------------------

            AuthenticationTicket ticket;
            TicketSerializer serializer = new TicketSerializer();

            IEnumerable<byte[]> values = null;

            switch (ASPNETIdentityConfig.UserStoreType)
            {
                case EnumUserStoreType.Memory:
                    if (RefreshTokenProvider.RefreshTokens.TryRemove(context.Token, out ticket))
                    {
                        context.SetTicket(ticket);
                    }
                    break;

                case EnumUserStoreType.SqlServer:
                case EnumUserStoreType.OracleMD:
                case EnumUserStoreType.PostgreSQL: // DMBMS

                    using (IDbConnection cnn = DataAccess.CreateConnection())
                    {
                        cnn.Open();

                        switch (ASPNETIdentityConfig.UserStoreType)
                        {
                            case EnumUserStoreType.SqlServer:

                                values = cnn.Query<byte[]>(
                                    "SELECT [Value] FROM [RefreshTokenDictionary] WHERE [Key] = @Key", new { Key = context.Token });

                                ticket = serializer.Deserialize(values.AsList()[0]);
                                context.SetTicket(ticket);

                                cnn.Execute(
                                    "DELETE FROM [RefreshTokenDictionary] WHERE [Key] = @Key", new { Key = context.Token });

                                break;

                            case EnumUserStoreType.OracleMD:

                                values = cnn.Query<byte[]>(
                                    "SELECT \"Value\" FROM \"RefreshTokenDictionary\" WHERE \"Key\" = :Key", new { Key = context.Token });

                                ticket = serializer.Deserialize(values.AsList()[0]);
                                context.SetTicket(ticket);

                                cnn.Execute(
                                    "DELETE FROM \"RefreshTokenDictionary\" WHERE \"Key\" = :Key", new { Key = context.Token });

                                break;

                            case EnumUserStoreType.PostgreSQL:

                                break;

                        }
                    }

                    break;
            }
        }

        #endregion
    }
}