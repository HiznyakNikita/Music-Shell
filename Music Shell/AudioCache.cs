using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Music_Shell
{
    public static class AudioCache
    {
        public static bool IsInCache(string audioTitle)
        {
            return File.Exists(Properties.Settings.Default.cachePath + "/" + audioTitle + ".mp3");
        }

        public static void GetAudio(ListBox lb, User user)
        {
            string[] audios = Directory.GetFiles(Properties.Settings.Default.cachePath);
            int pathLength = Properties.Settings.Default.cachePath.Length;
            user.tracks = new List<Track>();
            foreach (var item in audios)
            {
                string songName = item.Substring(pathLength + 1, item.Length - pathLength - 5);
                string[] nameParts = songName.Split('-');
                lb.Items.Add(songName);
                user.tracks.Add(new Track()
                    {
                      artist = nameParts[0],
                      title = nameParts[1]
                    }
                    );
            }
        }
    }
}
