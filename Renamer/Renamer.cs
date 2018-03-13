using System;
using System.Text;//for Stringbuilder
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
    //calls the other method
    //videolocal is unique file details in general
    //videolocal_place is instance of unique file somehwere in filesystem
    public string GetFileName(SVR_VideoLocal_Place video) => GetFileName(video.VideoLocal);
    
    //so, file in general
    public string GetFileName(SVR_VideoLocal video)
    {
      //get anidb info about the file
      var file = video.GetAniDBFile();
      //get first episode info
      var episode = video.GetAnimeEpisodes()[0].AniDB_Episode;
      //get anime info
      var anime = RepoFactory.AniDB_Anime.GetByAnimeID(episode.AnimeID);
      
      //file name we are putting together
      StringBuilder name = new StringBuilder();
      
      if (!string.IsNullOrWhiteSpace(file.Anime_GroupNameShort)) {
        //have releasing group name
        name.Append($"[{file.Anime_GroupNameShort}]");
      }
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
        name.Append($" Censored");
      //dont care about source
      if (file.File_Source != null &&
          (file.File_Source.Equals("DVD", StringComparison.InvariantCultureIgnoreCase) ||
          file.File_Source.Equals("Blu-ray", StringComparison.InvariantCultureIgnoreCase))) {
//        name.Append($" {file.File_Source}");
      }
      //then add resolution
//      name.Append($" ({video.VideoResolution}");
      int height = Utils.GetVideoHeight(file.File_VideoResolution);
      if (0 == height)
        height = Utils.GetVideoHeight(video.VideoResolution);
      name.Append($" [{height}p]"); //that's not very nice
      //video encoding
//      name.Append($" {(file?.File_VideoCodec ?? video.VideoCodec).Replace("\\", "").Replace("/", "")}".TrimEnd());
      //don't care about bits
      if (video.VideoBitDepth == "10") {
//        name.Append($" {video.VideoBitDepth}bit");
      }
      //resolution end
//      name.Append(')');

      name.Append($" [{video.CRC32.ToUpper()}]");
      name.Append($"{System.IO.Path.GetExtension(video.FileName)}");
      
      return Utils.ReplaceInvalidFolderNameCharacters(name.ToString());
    }
    
    //number to text and prepend zeroes
    string PadNumberTo(int number, int max, char padWith = '0') {
      return number.ToString().PadLeft(max.ToString().Length, padWith);
    }
    
    //where does this file go
    public (ImportFolder dest, string folder) GetDestinationFolder(SVR_VideoLocal_Place video) {
      StringBuilder folder = new StringBuilder();
      var anime = RepoFactory.AniDB_Anime.GetByAnimeID(video.VideoLocal.GetAnimeEpisodes()[0].AniDB_Episode.AnimeID);
      bool IsPorn = anime.Restricted > 0;

      //series file goes inside 'year/month/series' folder
//      folder.Append($"{anime.BeginYear}/");
      DateTime date = (DateTime)anime.AirDate;
      if (null != date) {
        if (1 < date.Year) {
          folder.Append($"{date.Year}/");
          if (0 < date.Month)
            folder.Append($"{PadNumberTo(date.Month, 12)}/");
        }
      }
      folder.Append($"{Utils.ReplaceInvalidFolderNameCharacters(anime.PreferredTitle)}");

      ImportFolder dest = RepoFactory.ImportFolder.GetByImportLocation(IsPorn ? "/_Hentai/" : "/_Animes/");
//      ImportFolder dest = RepoFactory.ImportFolder.GetByImportLocation("/_Animes/");
          
      return (dest, folder.ToString());
    }
  }
}
