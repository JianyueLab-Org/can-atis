// <copyright file="SettingsDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Can.Network;
using CanAtis.Config;
using CanAtis.Events;
using CanAtis.Events.EventBus;

namespace CanAtis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the settings dialog, managing user settings and interaction logic.
/// </summary>
public class SettingsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;
    private string? _name;
    private string? _userId;
    private string? _password;
    private bool _muteOwnAtisUpdateSound;
    private bool _muteSharedAtisUpdateSound;
    private bool _autoFetchAtisLetter;
    private bool _autoFetchDatis;
    private bool _stationListOnTop;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialogViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration interface providing settings for the view model.</param>
    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        _appConfig = appConfig;

        Name = _appConfig.Name;
        UserId = _appConfig.UserId;
        Password = _appConfig.PasswordDecrypted;
        MuteOwnAtisUpdateSound = _appConfig.MuteOwnAtisUpdateSound;
        MuteSharedAtisUpdateSound = _appConfig.MuteSharedAtisUpdateSound;
        AutoFetchAtisLetter = _appConfig.AutoFetchAtisLetter;
        AutoFetchDatis = _appConfig.AutoFetchDatis;
        StationListOnTop = _appConfig.StationListOnTop;

        SaveSettingsCommand = ReactiveCommand.Create<ICloseable>(SaveSettings);
    }

    /// <summary>
    /// Gets the command used to save the user settings and close the associated dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveSettingsCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether station navigation is displayed along the top.
    /// </summary>
    public bool StationListOnTop
    {
        get => _stationListOnTop;
        set => this.RaiseAndSetIfChanged(ref _stationListOnTop, value);
    }

    /// <summary>
    /// Gets or sets the name associated with the current user configuration.
    /// </summary>
    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    /// <summary>
    /// Gets or sets the decrypted password associated with the settings dialog.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether notification sounds for the user's own ATIS updates should be muted.
    /// </summary>
    public bool MuteOwnAtisUpdateSound
    {
        get => _muteOwnAtisUpdateSound;
        set => this.RaiseAndSetIfChanged(ref _muteOwnAtisUpdateSound, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether notification sounds for shared ATIS updates should be muted.
    /// </summary>
    public bool MuteSharedAtisUpdateSound
    {
        get => _muteSharedAtisUpdateSound;
        set => this.RaiseAndSetIfChanged(ref _muteSharedAtisUpdateSound, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically fetch real-world ATIS letter.
    /// </summary>
    public bool AutoFetchAtisLetter
    {
        get => _autoFetchAtisLetter;
        set => this.RaiseAndSetIfChanged(ref _autoFetchAtisLetter, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically fetch and populate real-world D-ATIS data.
    /// When enabled, <see cref="AutoFetchAtisLetter"/> is forced on.
    /// </summary>
    public bool AutoFetchDatis
    {
        get => _autoFetchDatis;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoFetchDatis, value);
            if (value)
            {
                AutoFetchAtisLetter = true;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SaveSettingsCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void SaveSettings(ICloseable window)
    {
        _appConfig.Name = Name ?? "";
        _appConfig.UserId = UserId?.Trim() ?? "";
        _appConfig.PasswordDecrypted = Password?.Trim() ?? "";
        _appConfig.MuteOwnAtisUpdateSound = MuteOwnAtisUpdateSound;
        _appConfig.MuteSharedAtisUpdateSound = MuteSharedAtisUpdateSound;
        _appConfig.AutoFetchAtisLetter = AutoFetchAtisLetter;
        _appConfig.AutoFetchDatis = AutoFetchDatis;
        _appConfig.StationListOnTop = StationListOnTop;

        _appConfig.NetworkRating = NetworkRating.Obs;

        _appConfig.SaveConfig();

        EventBus.Instance.Publish(new GeneralSettingsUpdated());

        window.Close();
    }
}
