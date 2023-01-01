﻿using System;
using System.Threading;
using System.Threading.Tasks;
using IPA.Loader;
using SaberFactory.Configuration;
using SaberFactory.HarmonyPatches;
using SaberFactory.Helpers;
using SaberFactory.Instances;
using SaberFactory.Models;
using SaberFactory.UI;
using SaberFactory.UI.Lib;
using SiraUtil.Logging;
using SiraUtil.Tools;
using Tweening;
using UnityEngine;
using Zenject;

namespace SaberFactory.Editor
{
    /// <summary>
    ///     Class for managing the presentation of the saber factory editor
    /// </summary>
    internal class LegacyEditor : IInitializable, IDisposable
    {
        public static LegacyEditor Instance;
        public bool IsActive { get; private set; }

        public bool IsSaberInHand
        {
            get => _isSaberInHand;
            set
            {
                _isSaberInHand = value;
                _saberInstanceManager.Refresh();
            }
        }

        //private readonly BaseUiComposition _baseUiComposition;

        private readonly SaberInstanceManager _saberInstanceManager;

        private readonly SiraLog _logger;
        private readonly MenuSaberProvider _menuSaberProvider;

        private readonly LegacyPedestal _legacyPedestal;
        private readonly PlayerDataModel _playerDataModel;
        private readonly PluginConfig _pluginConfig;
        private readonly SaberGrabController _saberGrabController;
        private readonly SaberSet _saberSet;
        private readonly SFLogoAnim _sfLogoAnim;
        private bool _isFirstActivation = true;
        private bool _isSaberInHand;
        private SaberInstance _spawnedSaber;
        private readonly PluginMetadata _metaData;
        private readonly TimeTweeningManager _tweeningManager;

        private LegacyEditor(
            SiraLog logger,
            PluginConfig pluginConfig,
            SaberInstanceManager saberInstanceManager,
            EmbeddedAssetLoader embeddedAssetLoader,
            SaberSet saberSet,
            PlayerDataModel playerDataModel,
            SaberGrabController saberGrabController,
            MenuSaberProvider menuSaberProvider,
            PluginDirectories pluginDirs,
            [Inject(Id = nameof(SaberFactory))]PluginMetadata metadata,
            TimeTweeningManager tweeningManager)
        {
            _logger = logger;
            _metaData = metadata;
            _tweeningManager = tweeningManager;
            _pluginConfig = pluginConfig;
            _saberInstanceManager = saberInstanceManager;
            _saberSet = saberSet;
            _playerDataModel = playerDataModel;
            _saberGrabController = saberGrabController;
            _menuSaberProvider = menuSaberProvider;

            _legacyPedestal = new LegacyPedestal(pluginDirs.SaberFactoryDir.GetFile("pedestal"));
            _sfLogoAnim = new SFLogoAnim(embeddedAssetLoader);

            Instance = this;
            GameplaySetupViewPatch.EntryEnabled = _pluginConfig.ShowGameplaySettingsButton;
        }

        public void Dispose()
        {
            Instance = null;

            _legacyPedestal.Destroy();
        }

        public async void Initialize()
        {

            // Create Pedestal
            var pos = new Vector3(0.3f, 0, 0.9f);
            await _legacyPedestal.Instantiate(pos, Quaternion.Euler(0, 25, 0));
            SetPedestalText(1, "<color=#ffffff70>SF v"+_metaData.HVersion+"</color>");
#if PAT
            SetPedestalText(2, "<color=#ffffff80>Patreon ♥</color>");
#endif
            SetupGlobalShaderVars();
        }

        public async void Open()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;

            _saberInstanceManager.OnModelCompositionSet += OnModelCompositionSet;

            _legacyPedestal.IsVisible = true;

            _saberInstanceManager.Refresh();


            if (_isFirstActivation && _pluginConfig.RuntimeFirstLaunch)
            {
                await _sfLogoAnim.Instantiate(new Vector3(-1, -0.04f, 2), Quaternion.Euler(0, 45, 0));
                await _sfLogoAnim.PlayAnim();
            }

            _menuSaberProvider.RequestSaberVisiblity(false);

            _isFirstActivation = false;
        }

        public void Close()
        {
            Close(false);
        }

        public void Close(bool instant)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;

            _saberInstanceManager.SyncSabers();
            _saberInstanceManager.OnModelCompositionSet -= OnModelCompositionSet;
            _saberInstanceManager.DestroySaber();
            _spawnedSaber?.Destroy();

            _legacyPedestal.IsVisible = false;


            _saberGrabController.ShowHandle();

            _menuSaberProvider.RequestSaberVisiblity(true);
        }

        public void SetPedestalText(int line, string text)
        {
            _legacyPedestal.SetText(line, text);
        }

        public void FlashPedestal(Color color)
        {
            _tweeningManager.KillAllTweens(_legacyPedestal.SaberContainerTransform);
            
            _tweeningManager.AddTween(new FloatTween(1, 0, f =>
            {
                _legacyPedestal.SetLedColor(color.ColorWithAlpha(f));
            }, 1, EaseType.InCubic), _legacyPedestal.SaberContainerTransform);

            _legacyPedestal.InitSpiral();
            _tweeningManager.AddTween(new FloatTween(-1, 1, f =>
            {
                _legacyPedestal.SetSpiralLength(f);
            }, 1, EaseType.OutCubic), _legacyPedestal.SaberContainerTransform);
        }

        private async void OnModelCompositionSet(ModelComposition composition)
        {
            _spawnedSaber?.Destroy();

            var parent = IsSaberInHand ? _saberGrabController.GrabContainer : _legacyPedestal.SaberContainerTransform;

            _spawnedSaber = _saberInstanceManager.CreateSaber(_saberSet.LeftSaber, parent);

            if (IsSaberInHand)
            {
                _spawnedSaber.CreateTrail(true);
                _saberGrabController.HideHandle();
            }
            else
            {
                _saberGrabController.ShowHandle();
            }

            _spawnedSaber.SetColor(_playerDataModel.playerData.colorSchemesSettings.GetSelectedColorScheme().saberAColor);

            _saberInstanceManager.RaiseSaberCreatedEvent();
            _saberInstanceManager.RaisePieceCreatedEvent();

            await Task.Yield();

            if (_pluginConfig.AnimateSaberSelection)
            {
                await AnimationHelper.AsyncAnimation(0.3f, CancellationToken.None, t => { parent.localScale = new Vector3(t, t, t); });
            }
        }

        private void SetupGlobalShaderVars()
        {
            var scheme = _playerDataModel.playerData.colorSchemesSettings.GetSelectedColorScheme();
            Shader.SetGlobalColor(MaterialProperties.UserColorLeft, scheme.saberAColor);
            Shader.SetGlobalColor(MaterialProperties.UserColorRight, scheme.saberBColor);
        }
    }
}