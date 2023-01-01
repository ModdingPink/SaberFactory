﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BeatSaberMarkupLanguage;
using SaberFactory.Loaders;
using SaberFactory.UI;
using SaberFactory.UI.Flow;
using SaberFactory.UI.Lib;
using TMPro;
using UnityEngine;

namespace SaberFactory.Models
{
    public class PreloadMetaData : IAssetInfo
    {
        public AssetTypeDefinition AssetTypeDefinition { get; private set; }

        public Texture2D CoverTex
        {
            get
            {
                if (_coverTex == null)
                {
                    _coverTex = LoadTexture();
                }

                return _coverTex;
            }
        }

        public Sprite CoverSprite
        {
            get
            {
                if (_coverSprite == null)
                {
                    _coverSprite = LoadSprite();
                }

                return _coverSprite;
            }
        }

        internal readonly AssetMetaPath AssetMetaPath;

        private byte[] _coverData;
        private Sprite _coverSprite;
        private Texture2D _coverTex;

        internal PreloadMetaData(AssetMetaPath assetMetaPath)
        {
            AssetMetaPath = assetMetaPath;
        }

        internal PreloadMetaData(AssetMetaPath assetMetaPath, IAssetInfo customListItem, AssetTypeDefinition assetTypeDefinition)
        {
            AssetMetaPath = assetMetaPath;
            AssetTypeDefinition = assetTypeDefinition;
            Name = customListItem.Name;
            Author = customListItem.Author;
            _coverSprite = customListItem.Cover;
        }

        public string Name { get; private set; }

        public string Author { get; private set; }

        public Sprite Cover => CoverSprite;

        public bool IsFavorite { get; set; }

        public string SubDir => AssetMetaPath.SubDirName;

        public void SaveToFile()
        {
            if (AssetMetaPath.HasMetaData)
            {
                File.Delete(AssetMetaPath.MetaDataPath);
            }

            var ser = new SerializableMeta();
            ser.Name = Name;
            ser.Author = Author;
            ser.AssetTypeDefinition = AssetTypeDefinition;

            if (_coverSprite != null)
            {
                var tex = _coverSprite.texture;
                ser.CoverData = GetTextureData(tex);
            }

            var fs = new FileStream(AssetMetaPath.MetaDataPath, FileMode.Create, FileAccess.Write, FileShare.Write);
            var formatter = new BinaryFormatter();
            formatter.Serialize(fs, ser);
            fs.Close();
        }

        public void LoadFromFile()
        {
            LoadFromFile(AssetMetaPath.MetaDataPath);
        }

        public void LoadFromFile(string path)
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var formatter = new BinaryFormatter();
            var ser = (SerializableMeta)formatter.Deserialize(fs);
            fs.Close();

            Name = ser.Name;
            Author = ser.Author;
            _coverData = ser.CoverData;
            AssetTypeDefinition = ser.AssetTypeDefinition;

            LoadSprite();
        }

        public void SetFavorite(bool isFavorite)
        {
            IsFavorite = isFavorite;
        }

        /// <summary>
        ///     Get Texture png data from non-readable texture
        /// </summary>
        /// <param name="tex">The texture to read from</param>
        /// <returns>png bytes</returns>
        private byte[] GetTextureData(Texture2D tex)
        {
            var tmp = RenderTexture.GetTemporary(
                tex.width,
                tex.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);

            Graphics.Blit(tex, tmp);

            var previous = RenderTexture.active;
            RenderTexture.active = tmp;
            var myTexture2D = new Texture2D(tex.width, tex.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D.EncodeToPNG();
        }

        private Texture2D LoadTexture()
        {
            return _coverData == null ? null : Utilities.LoadTextureRaw(_coverData);
        }

        private Sprite LoadSprite()
        {
            return CoverTex == null ? null : Utilities.LoadSpriteFromTexture(CoverTex);
        }

        [Serializable]
        internal class SerializableMeta
        {
            public AssetTypeDefinition AssetTypeDefinition;
            public string Author;
            public byte[] CoverData;
            public string Name;
        }
    }
}