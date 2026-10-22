// <copyright file="ViewModelFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using CanAtis.Atis;
using CanAtis.Config;
using CanAtis.NavData;
using CanAtis.Networking.AtisHub;
using CanAtis.Profiles;
using CanAtis.Profiles.Models;
using CanAtis.Sessions;
using CanAtis.Ui;
using CanAtis.Ui.Controls.Notification;
using CanAtis.Ui.Services.Websocket;
using CanAtis.Ui.ViewModels;
using CanAtis.Ui.ViewModels.AtisConfiguration;
using CanAtis.Weather;

namespace CanAtis.Container.Factory;

/// <summary>
/// Factory for creating view models.
/// </summary>
internal class ViewModelFactory : IViewModelFactory
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public ViewModelFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AtisStationViewModel"/> class.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <param name="windowNotificationManager">The window notification manager.</param>
    /// <returns>A new <see cref="AtisStationViewModel"/> instance.</returns>
    public AtisStationViewModel CreateAtisStationViewModel(AtisStation station,
        WindowNotificationManager? windowNotificationManager)
    {
        return new AtisStationViewModel(
            station,
            windowNotificationManager,
            _provider.GetService<INetworkConnectionFactory>(),
            _provider.GetService<IVoiceServerConnectionFactory>(),
            _provider.GetService<IAppConfig>(),
            _provider.GetService<IAtisBuilder>(),
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<INavDataRepository>(),
            _provider.GetService<IAtisHubConnection>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<IWebsocketService>(),
            _provider.GetService<IDatisRepository>());
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ContractionsViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="ContractionsViewModel"/> instance.</returns>
    public ContractionsViewModel CreateContractionsViewModel()
    {
        return new ContractionsViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IAppConfig>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>());
    }

    /// <summary>
    /// Creates a new instance of the <see cref="FormattingViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="FormattingViewModel"/> instance.</returns>
    public FormattingViewModel CreateFormattingViewModel()
    {
        return new FormattingViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="GeneralConfigViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="GeneralConfigViewModel"/> instance.</returns>
    public GeneralConfigViewModel CreateGeneralConfigViewModel()
    {
        return new GeneralConfigViewModel(
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>());
    }

    /// <summary>
    /// Creates and returns an instance of the <see cref="PresetsViewModel"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="PresetsViewModel"/> class.</returns>
    public PresetsViewModel CreatePresetsViewModel()
    {
        return new PresetsViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IAtisBuilder>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="SandboxViewModel"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="SandboxViewModel"/> class.</returns>
    public SandboxViewModel CreateSandboxViewModel()
    {
        return new SandboxViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IAtisBuilder>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IDatisRepository>());
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DatisReplacementsViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="DatisReplacementsViewModel"/> instance.</returns>
    public DatisReplacementsViewModel CreateDatisReplacementsViewModel()
    {
        return new DatisReplacementsViewModel(
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }
}
