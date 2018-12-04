﻿//**********************************************************************************
//* テンプレート
//**********************************************************************************

// 以下のLicenseに従い、このProjectをTemplateとして使用可能です。Release時にCopyright表示してSublicenseして下さい。
// https://github.com/OpenTouryoProject/MultiPurposeAuthSite/blob/master/license/LicenseForTemplates.txt

//**********************************************************************************
//* クラス名        ：RolesAdminEditViewModel
//* クラス日本語名  ：ロール管理画面用のVM（テンプレート）
//*
//* 作成日時        ：－
//* 作成者          ：－
//* 更新履歴        ：－
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2017/04/24  西野 大介         新規
//**********************************************************************************

using MultiPurposeAuthSite.Co;

using System.ComponentModel.DataAnnotations;

/// <summary>MultiPurposeAuthSite.ViewModels</summary>
namespace MultiPurposeAuthSite.ViewModels
{
    /// <summary>ロール管理画面用のVM（テンプレート）</summary>
    public class RolesAdminEditViewModel : BaseViewModel
    {
        /// <summary>ロールID</summary>
        //[Required(AllowEmptyStrings = false)] // Create時は存在しない。
        // [StringLength( // ユーザ入力でないので不要
        public string Id { get; set; }
        
        /// <summary>ロール名</summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "RoleName", ResourceType = typeof(Resources.CommonViewModels))]
        [StringLength(
            Const.MaxLengthOfRoleName,
            ErrorMessageResourceName = "MaxLengthErrMsg",
            ErrorMessageResourceType = typeof(Resources.CommonViewModels))]
        public string Name { get; set; }
    }
}