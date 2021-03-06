﻿//**********************************************************************************
//* テンプレート
//**********************************************************************************

// 以下のLicenseに従い、このProjectをTemplateとして使用可能です。Release時にCopyright表示してSublicenseして下さい。
// https://github.com/OpenTouryoProject/MultiPurposeAuthSite/blob/master/license/LicenseForTemplates.txt

//**********************************************************************************
//* クラス名        ：ManageLoginsViewModel
//* クラス日本語名  ：外部ログインの管理画面用のVM（テンプレート）
//*
//* 作成日時        ：－
//* 作成者          ：－
//* 更新履歴        ：－
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2017/04/24  西野 大介         新規
//**********************************************************************************

using System.Collections.Generic;

#if NETFX
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
#else
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
#endif

/// <summary>MultiPurposeAuthSite.ViewModels</summary>
namespace MultiPurposeAuthSite.ViewModels
{
    /// <summary>外部ログインの管理画面用のVM</summary>
    public class ManageLoginsViewModel : BaseViewModel
    {
        /// <summary>CurrentLogins</summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        /// <summary>OtherLogins</summary>
#if NETFX
        public IList<AuthenticationDescription> OtherLogins { get; set; }
#else
        public IList<AuthenticationScheme> OtherLogins { get; set; }
#endif
    }
}