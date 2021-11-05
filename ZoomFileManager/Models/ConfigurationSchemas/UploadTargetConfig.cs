﻿using System;

namespace ZoomFileManager.Models.ConfigurationSchemas
{
    public class UploadTargetConfig
    {
        private DropBoxClientConfig? _dropBoxClientConfig;
        private OD_DriveClientConfig? _oneDriveClientConfig;
        private OD_UserClientConfig? _oneDriveUserClientConfig;
        private SharepointClientConfig? _sharepointClientConfig;
        public string? Identifier { get; set; }
        public JobType Type { get; set; }
        public string? RootPath { get; set; }

        public dynamic? ClientConfig
        {
            get => Type switch
            {
                JobType.Dropbox => _dropBoxClientConfig,
                JobType.Sharepoint => _sharepointClientConfig,
                JobType.OnedriveDrive => _oneDriveClientConfig,
                JobType.OnedriveUser => _oneDriveUserClientConfig,
                _ => throw new ArgumentOutOfRangeException()
            };
            set
            {
                switch (Type)
                {
                    case JobType.Dropbox:
                        _dropBoxClientConfig = value;
                        break;
                    case JobType.OnedriveDrive:
                        _oneDriveClientConfig = value;
                        break;
                    case JobType.OnedriveUser:
                        _oneDriveUserClientConfig = value;
                        break;
                    case JobType.Sharepoint:
                        _sharepointClientConfig = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // public DropBoxClientConfig? DropBoxClientConfig
        // {
        //     get => ValidateConfigCombo() ? (_dropBoxClientConfig) : throw new ArgumentException();
        //
        //     set => _dropBoxClientConfig = value;
        // }
        //
        // public OD_DriveClientConfig? OneDriveClientConfig
        // {
        //     get => ValidateConfigCombo() ? _oneDriveClientConfig : throw new ArgumentException();
        //     set => _oneDriveClientConfig = value;
        // }
        //
        // public OD_UserClientConfig? OneDriveUserClientConfig
        // {
        //     get => ValidateConfigCombo() ? _oneDriveUserClientConfig : throw new ArgumentException();
        //     set => _oneDriveUserClientConfig = value;
        // }
        //
        // public SharepointClientConfig? SharepointClientConfig
        // {
        //     get => ValidateConfigCombo() ? _sharepointClientConfig : throw new ArgumentException();
        //     set => _sharepointClientConfig = value;
        // }

        // private bool ValidateConfigCombo()
        // {
        //     var c = 0;
        //     if (_dropBoxClientConfig == null) c++;
        //     if (_oneDriveClientConfig == null) c++;
        //     if (_sharepointClientConfig == null) c++;
        //     if (_oneDriveUserClientConfig == null) c++;
        //     return c == 1;
        // }
    }
}