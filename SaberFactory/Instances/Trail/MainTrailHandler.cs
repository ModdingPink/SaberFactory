﻿using IPA.Utilities;
using SaberFactory.Configuration;
using SaberFactory.Helpers;
using UnityEngine;

namespace SaberFactory.Instances.Trail
{
    internal class MainTrailHandler : ITrailHandler
    {
        private readonly PlayerTransforms _playerTransforms;
        private readonly SaberSettableSettings _saberSettableSettings;
        public SFTrail TrailInstance { get; }

        protected InstanceTrailData InstanceTrailData;

        private readonly SaberTrail _backupTrail;
        private bool _canColorMaterial;

        public MainTrailHandler(GameObject gameobject, PlayerTransforms playerTransforms, SaberSettableSettings saberSettableSettings)
        {
            _playerTransforms = playerTransforms;
            _saberSettableSettings = saberSettableSettings;
            TrailInstance = gameobject.AddComponent<SFTrail>();
        }

        public MainTrailHandler(GameObject gameobject, SaberTrail backupTrail, PlayerTransforms playerTransforms, SaberSettableSettings saberSettableSettings) : this(gameobject, playerTransforms, saberSettableSettings)
        {
            _backupTrail = backupTrail;
        }

        public void CreateTrail(TrailConfig trailConfig, bool editor)
        {
            if (InstanceTrailData is null)
            {
                if (_backupTrail is null)
                {
                    return;
                }

                var trailStart = TrailInstance.gameObject.CreateGameObject("Trail StartNew");
                var trailEnd = TrailInstance.gameObject.CreateGameObject("TrailEnd");
                trailEnd.transform.localPosition = new Vector3(0, 0, 1);

                var trailRenderer = _backupTrail.GetField<SaberTrailRenderer, SaberTrail>("_trailRendererPrefab");

                var material = trailRenderer.GetField<MeshRenderer, SaberTrailRenderer>("_meshRenderer").material;

                var trailInitDataVanilla = new TrailInitData
                {
                    TrailColor = Color.white,
                    TrailLength = 15,
                    Whitestep = 0.02f,
                    Granularity = trailConfig.Granularity
                };

                TrailInstance.Setup(trailInitDataVanilla, trailStart.transform, trailEnd.transform, material, editor);
                TrailInstance.PlayerTransforms = _playerTransforms;
                InitSettableSettings();
                
                return;
            }

            if (InstanceTrailData.Length.Value < 1)
            {
                return;
            }

            var trailInitData = new TrailInitData
            {
                TrailColor = Color.white,
                TrailLength = InstanceTrailData.Length.Value,
                Whitestep = InstanceTrailData.Whitestep.Value,
                Granularity = trailConfig.Granularity,
                SamplingFrequency = trailConfig.SamplingFrequency
            };

            var (pointStart, pointEnd) = InstanceTrailData.GetPoints();

            if (pointStart == null || pointEnd == null)
            {
                Debug.LogWarning("Primary trail on saber doesn't seem to have a positional transform");
                return;
            }

            TrailInstance.Setup(
                trailInitData,
                pointStart,
                pointEnd,
                InstanceTrailData.Material.Material,
                editor
            );
            TrailInstance.PlayerTransforms = _playerTransforms;
            InitSettableSettings();

            if (!trailConfig.OnlyUseVertexColor)
            {
                _canColorMaterial = MaterialHelpers.IsMaterialColorable(InstanceTrailData.Material.Material);
            }
        }
        
        private void UpdateRelativeMode()
        {
            TrailInstance.RelativeMode = _saberSettableSettings.RelativeTrailMode.Value;
        }

        private void InitSettableSettings()
        {
            if (_saberSettableSettings == null) return;

            UpdateRelativeMode();
            _saberSettableSettings.RelativeTrailMode.ValueChanged += UpdateRelativeMode;
        }

        private void UnInitSettableSettings()
        {
            if (_saberSettableSettings == null) return;

            _saberSettableSettings.RelativeTrailMode.ValueChanged -= UpdateRelativeMode;
        }

        public void DestroyTrail(bool immediate = false)
        {
            UnInitSettableSettings();
            
            if (immediate)
            {
                TrailInstance.TryDestoryImmediate();
            }
            else
            {
                TrailInstance.TryDestroy();
            }
        }

        public void SetTrailData(InstanceTrailData instanceTrailData)
        {
            InstanceTrailData = instanceTrailData;
        }

        public void SetColor(Color color)
        {
            if (TrailInstance is { })
            {
                TrailInstance.Color = color;
            }

            if (_canColorMaterial)
            {
                TrailInstance.SetMaterialBlock(MaterialHelpers.ColorBlock(color));
            }
        }
    }
}