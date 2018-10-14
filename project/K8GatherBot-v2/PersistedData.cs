using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;

namespace K8GatherBotv2
{
    public class PersistedData
    {
        private string fatkidFileName = "fatkid.csv";
        private string highScoreFileName = "highscores.csv";
        private string thinkidFileName = "thinkid.csv";
        private string captainsFileName = "captains.csv";

        private List<UserData> fatKids = new List<UserData>();
        private List<UserData> highScores = new List<UserData>();
        private List<UserData> thinKids = new List<UserData>();
        private List<UserData> captains = new List<UserData>();

        public PersistedData()
        {
            InitData();
        }

        private void InitData()
        {
            InitList(fatKids, fatkidFileName);
            InitList(thinKids, thinkidFileName);
            InitList(highScores, highScoreFileName);
            InitList(captains, captainsFileName);
        }

        private void InitList(List<UserData> data, string fileName)
        {
            if (!File.Exists(@fileName))
            {
                return;
            }
            using (TextReader fileReader = File.OpenText(@fileName))
            {
                CsvReader csvFile = new CsvReader(fileReader);
                csvFile.Configuration.HasHeaderRecord = false;
                csvFile.Read();
                var records = csvFile.GetRecords<UserData>();
                foreach (UserData pair in records)
                {
                    data.Add(pair);
                }
            }
        }

        private void PersistList(List<UserData> data, string fileName)
        {
            using (TextWriter writer = new StreamWriter(File.Open(@fileName, FileMode.Create)))
            {
                var csv = new CsvWriter(writer);
                csv.Configuration.Encoding = Encoding.UTF8;
                data.Sort();
                csv.WriteRecords(data);
                writer.Flush();
            }
        }

        public void AddFatKid(string id, string userName)
        {
            Add(fatKids, id, userName);
            PersistList(fatKids, fatkidFileName);
        }

        public void AddThinKid(string id, string userName)
        {
            Add(thinKids, id, userName);
            PersistList(thinKids, thinkidFileName);
        }

        public void AddHighScores(List<string> ids, List<string> userNames)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                Add(highScores, ids[i], userNames[i]);
            }
            PersistList(highScores, highScoreFileName);
        }

        public void AddCaptains(string cid, string cUserName1, string cid2, string cUserName2)
        {
            Add(captains, cid, cUserName1);
            Add(captains, cid2, cUserName2);
            PersistList(captains, captainsFileName);
        }

        public void AddCaptain(string cid, string cUserName1)
        {
            Add(captains, cid, cUserName1);
            PersistList(captains, captainsFileName);
        }

        public void RemoveCaptain(string cid, string cUserName)
        {
            Minus(captains, cid, cUserName);
            PersistList(captains, captainsFileName);
        }

        private void Minus(List<UserData> data, string id, string userName)
        {
            UserData entry = data.Find(x => x.id.Equals(id));
            if (entry != null)
            {
                entry.Minus();
            }
        }

        private void Add(List<UserData> data, string id, string userName)
        {
            UserData entry = data.Find(x => x.id.Equals(id));

            if (entry == null)
            {
                entry = new UserData(id, userName);
                data.Add(entry);
            }
            else
            {
                entry.userName = userName;
            }
            entry.Add();
        }

        public string GetFatKidInfo(Tuple<string, string> idUsername, string response)
        {
            return GetInfo(fatKids, idUsername, response);
        }

        public string GetThinKidInfo(Tuple<string, string> idUsername, string response)
        {
            return GetInfo(thinKids, idUsername, response);
        }

        public string GetHighScoreInfo(Tuple<string, string> idUsername, string response)
        {
            return GetInfo(highScores, idUsername, response);
        }

        public string GetCaptainInfo(Tuple<string, string> idUsername, string response)
        {
            return GetInfo(captains, idUsername, response);
        }

        private string GetInfo(List<UserData> data, Tuple<string, string> idUsername, string response)
        {
            UserData entry = null;
            if (idUsername.Item1 != null)
            {
                entry = data.Find(x => x.id.Equals(idUsername.Item1));
            }
            else
            {
                entry = data.Find(x => x.userName.Equals(idUsername.Item2));
            }

            if (entry == null)
            {
                return String.Format(response, idUsername.Item2, 0, data.Count, data.Count);
            }
            return String.Format(response, entry.userName, entry.count, data.IndexOf(entry) + 1, data.Count);
        }

        public string GetFatKidTop10()
        {
            return GetTop10Info(fatKids);
        }

        public string GetThinKidTop10()
        {
            return GetTop10Info(thinKids);
        }

        public string GetHighScoreTop10()
        {
            return GetTop10Info(highScores);
        }

        public string GetCaptainTop10()
        {
            return GetTop10Info(captains);
        }

        private string GetTop10Info(List<UserData> data)
        {
            if (data.Count == 0)
            {
                return ":(";
            }
            string list = "";
            for (int i = 0; i < 10 && i != data.Count; i++)
            {
                UserData entry = data[i];
                list += i + 1 + ". " + entry.userName + " / " + entry.count + "\n";
            }
            return list;
        }

        public bool IsNewKid(string id, int newKidThreshold)
        {
            int count = 0;
            var fkentry = fatKids.Find(x => x.id.Equals( id ));
            if (fkentry != null)
            {
                count += fkentry.count;
            }
            var tkentry = thinKids.Find(x => x.id.Equals( id ));
            if (tkentry != null)
            {
                count += tkentry.count;
            }
            return count < newKidThreshold;
        }

        public bool AreAllNewKids(List<string> ids, int newKidThreshold)
        {
            foreach (var id in ids)
            {
                if (!IsNewKid(id, newKidThreshold))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class UserData : IComparable<UserData>, IEquatable<UserData>
    {
        public string id { get; set; }
        public string userName { get; set; }
        public int count { get; set; }

        public UserData()
        {

        }

        public UserData(string id, string username)
        {
            this.id = id;
            this.userName = username;
            this.count = 0;
        }

        public UserData(string id, string username, int count)
        {
            this.id = id;
            this.userName = username;
            this.count = count;
        }

        public int CompareTo(UserData other)
        {
            if (this.count == other.count)
            {
                return string.Compare(this.userName, other.userName, StringComparison.OrdinalIgnoreCase);
            }
            return other.count.CompareTo(this.count);
        }

        public bool Equals(UserData other)
        {
            return this.id.Equals(other.id);
        }

        internal void Add()
        {
            count++;
        }

        internal void Minus()
        {
            count--;
        }
    }
}
