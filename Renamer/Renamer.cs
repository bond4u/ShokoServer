using System;
using System.Text;//for Stringbuilder
using NLog;
using Shoko.Commons.Extensions;
using Shoko.Models.Enums;
using Shoko.Models.Server;
using Shoko.Server;//for Utils?
using Shoko.Server.Renamer;//for Renamer interface
using Shoko.Server.Models;//for SVR_*
using Shoko.Server.Repositories;//for RepoFactory

namespace MyRenamer
{
  //this annotation is required
  [Renamer("MyRenamer", Description = "My Renamer")]
  public class Renamer : IRenamer
  {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    //calls the other method
    //videolocal is unique file details in general
    //videolocal_place is instance of unique file somehwere in filesystem
    public string GetFileName(SVR_VideoLocal_Place video) => GetFileName(video.VideoLocal);
    
    //so, file in general
    public string GetFileName(SVR_VideoLocal video)
    {
      //file name we are putting together
      StringBuilder name = new StringBuilder();
      try {
        //check out episodes
        var eps = video.GetAnimeEpisodes();
        if (null==eps) {
          logger.Warn("RenamerDll: GetFileName: Episodes list is null");
        } else if (0==eps.Count) {
          logger.Warn("RenamerDll: GetFileName: Episodes list is empty");
        }
        //get first episode info
        var episode = eps[0].AniDB_Episode;
        //get anidb info about the file
        var file = video.GetAniDBFile();
        if (null==file) {
          logger.Warn("RenamerDll: GetFileName: File is not known to AniDB");
        }
      
        if (!string.IsNullOrWhiteSpace(file.Anime_GroupNameShort)) {
          //have releasing group name
          name.Append($"[{file.Anime_GroupNameShort}]");
        } else {
          logger.Info("RenamerDll: GetFileName: no group info: "+file.FileName);
          if (file.FileName.StartsWith("[")) {
            int idx = file.FileName.IndexOf("]", StringComparison.Ordinal);
            if (0<=idx) {
              string grp = file.FileName.Substring(0,idx+1);
              logger.Info("RenamerDll: GetFileName: 1possible group: "+grp);
              name.Append(grp);
            }
          } else if (file.FileName.StartsWith("(")) {
            int idx = file.FileName.IndexOf(")", StringComparison.Ordinal);
            if (0<=idx) {
              string grp = file.FileName.Substring(0,idx+1);
              logger.Info("RenamerDll: GetFileName: 2possible group: "+grp);
              name.Append(grp);
            }
          }
        }
        //get anime info
        var anime = RepoFactory.AniDB_Anime.GetByAnimeID(episode.AnimeID);
        //then episode name/title
        name.Append($" {anime.PreferredTitle}");
    
        //what kind of file is it
        if (anime.AnimeType == (int) AnimeType.Movie) {
          //movie has a year
          name.Append($" ({anime.BeginYear}) ");
        } else { //it's not movie
          string prefix = "";
          if (anime.AnimeType == (int) AnimeType.OVA) {
            prefix = "OVA";
          } else { //not ova
            if (episode.GetEpisodeTypeEnum() == EpisodeType.Credits) prefix = "C";
            if (episode.GetEpisodeTypeEnum() == EpisodeType.Other) prefix = "O";
            if (episode.GetEpisodeTypeEnum() == EpisodeType.Parody) prefix = "P";
            if (episode.GetEpisodeTypeEnum() == EpisodeType.Special) prefix = "S";
            if (episode.GetEpisodeTypeEnum() == EpisodeType.Trailer) prefix = "T";
          }
          int epCount = 1;
          //how many episodes are there in total
          if (episode.GetEpisodeTypeEnum() == EpisodeType.Episode) epCount = anime.EpisodeCountNormal;
          if (episode.GetEpisodeTypeEnum() == EpisodeType.Special) epCount = anime.EpisodeCountSpecial;
          //add eps/creds number & prepend zeroes
          name.Append($" - {prefix}{PadNumberTo(episode.EpisodeNumber, epCount)}");
        }

        //is there a version
        if (1<file.FileVersion)
          name.Append($"v{file.FileVersion}");
        //is it censored
        if (0<file.IsCensored)
          name.Append(" Censored");
        //dont care about source
//      if (null!=file.File_Source &&
//          (file.File_Source.Equals("DVD", StringComparison.InvariantCultureIgnoreCase) ||
//          file.File_Source.Equals("Blu-ray", StringComparison.InvariantCultureIgnoreCase))) {
//        name.Append($" {file.File_Source}");
//      }
        //then add resolution
//      name.Append($" ({video.VideoResolution}");
        int height = Utils.GetVideoHeight(file.File_VideoResolution);
        if (0 == height)
          height = Utils.GetVideoHeight(video.VideoResolution);
        name.Append($" [{height}p]"); //that's not very nice
        //video encoding
//      name.Append($" {(file?.File_VideoCodec ?? video.VideoCodec).Replace("\\", "").Replace("/", "")}".TrimEnd());
        //don't care about bits
//      if (video.VideoBitDepth == "10") {
//        name.Append($" {video.VideoBitDepth}bit");
//      }
        //resolution end
//      name.Append(')');

        name.Append($"[{video.CRC32.ToUpper()}]");
        name.Append($"{System.IO.Path.GetExtension(video.FileName)}");
      } catch (Exception ex) {
        logger.Error(ex, "RenamerDll: GetFileName: Exception: "+ex.Message);
        throw ex;
      }
      return Utils.ReplaceInvalidFolderNameCharacters(name.ToString());
    }
    
    //number to text and prepend zeroes
    string PadNumberTo(int number, int max, char padWith = '0') {
      return number.ToString().PadLeft(max.ToString().Length, padWith);
    }
    
    //where does this file go
    public (ImportFolder dest, string folder) GetDestinationFolder(SVR_VideoLocal_Place video) {
      ImportFolder dest = null;
      StringBuilder folder = new StringBuilder();
      try {
        var eps = video.VideoLocal.GetAnimeEpisodes();
        if (null==eps) {
          logger.Warn("RenamerDll: GetDestFolder: eps list is null");
        } else if (0==eps.Count) {
          logger.Warn("RenamerDll: GetDestFolder: eps list is empty");
        }
        var anime = RepoFactory.AniDB_Anime.GetByAnimeID(eps[0].AniDB_Episode.AnimeID);
        bool isPorn = anime.Restricted > 0;

        //series file goes inside 'year/month/series' folder
//      folder.Append($"{anime.BeginYear}/");
        DateTime? date = anime.AirDate;
        if (null != date) {
          if (1 < date?.Year) {
            folder.Append($"{date?.Year}");
            folder.Append(System.IO.Path.DirectorySeparatorChar);
            if (0 < date?.Month) {
              folder.Append($"{PadNumberTo((int)date?.Month, 12)}");
              folder.Append(System.IO.Path.DirectorySeparatorChar);
            }
          }
        }
        folder.Append($"{Utils.ReplaceInvalidFolderNameCharacters(anime.PreferredTitle)}");
        bool isUnix = Environment.OSVersion.Platform.HasFlag(PlatformID.Unix);
        string path = "";
        if (isUnix) {
          path += System.IO.Path.DirectorySeparatorChar;
          if (isPorn) {
            path += "_Hentai";
          } else {
            path += "_Animes";
          }
        } else {
          path += "G"+System.IO.Path.VolumeSeparatorChar+System.IO.Path.DirectorySeparatorChar;
          path += "_torrentz"+System.IO.Path.DirectorySeparatorChar+"_sorted";
        }
        path += System.IO.Path.DirectorySeparatorChar;
        dest = RepoFactory.ImportFolder.GetByImportLocation(path);
      } catch (Exception ex) {
        logger.Error(ex,"RenamerDll: GetDestFolder: Exception: "+ex.Message);
        throw ex;
      }
      return (dest, folder.ToString());
    }
  } // class Renamer
} // ns MyRenamer
