﻿using System;
using System.Xml;

namespace AniDBAPI
{
    [Serializable]
    public class Raw_AniDB_Anime_Title : XMLBase
    {
        #region Properties

        public int AnimeID { get; set; }
        public string TitleType { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }

        #endregion

        public Raw_AniDB_Anime_Title()
        {
        }

        public void ProcessFromHTTPResult(XmlNode node, int anid)
        {
            this.AnimeID = anid;
            this.TitleType = string.Empty;
            this.Language = string.Empty;
            this.Title = string.Empty;

            this.TitleType = AniDBHTTPHelper.TryGetAttribute(node, "type");
            this.Language = AniDBHTTPHelper.TryGetAttribute(node, "xml:lang");
            this.Title = node.InnerText.Trim().Replace('`', '\'');

            // Title Types
            // -------------
            // main
            // official
            // syn / SYNONYM / SYNONYMs
            // short

            // Common Languages
            // en = english
            // x-jat = romaji
            // ja = kanji
        }
    }
}