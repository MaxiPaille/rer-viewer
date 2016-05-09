using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class ScheduleManager : MonoBehaviour 
{

    private List<DateTime> _trainSchedules;

    public Transform[] Entries;

    public int RushTime;
    public int DeadTime;

    public Color DeadColor;
    public Color RushColor;
    public Color CoolColor;

    private bool _updating = false;

    private DateTime _lastAutoUpdate = DateTime.Now;

    void Start()
    {
        StartCoroutine(UpdateSchedules());
    }

    public void ForceRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(UpdateSchedules());
    }

    IEnumerator UpdateSchedules()
    {
        _updating = true;
        // A - http://www.ratp.fr/horaires/fr/ratp/rer/prochains_passages/RA/Nogent+Sur+Marne/A
        // B - http://www.ratp.fr/horaires/fr/ratp/rer/prochains_passages/RB/Bourg+la+Reine/A

        WWW request = new WWW(@"http://www.ratp.fr/horaires/fr/ratp/rer/prochains_passages/RA/Nogent+Sur+Marne/A");
        yield return new WaitWhile(() => !request.isDone);

        string result = request.text;

        string key = "<span class=\"direction\">";
        int keyPosition = result.IndexOf(key);

        if(keyPosition >= 0)
        {
            string subResult = result.Substring(keyPosition);

            key = "<table>";
            subResult = subResult.Substring(subResult.IndexOf(key));

            key = "</table>";
            subResult = subResult.Substring(0, subResult.IndexOf(key) + key.Length);

            Regex regexTR = new Regex("<tr[a-zA-Z \"=]*>(.*?)<\\/tr>", RegexOptions.Singleline);
            MatchCollection matchesTR = regexTR.Matches(subResult);

            _trainSchedules = new List<DateTime>();

            for (int i = 1; i < matchesTR.Count; i++)
            {
                string tr = matchesTR[i].Value;

                Regex regexName = new Regex("<td class=\"name\">.*<a.*?>([a-zA-Z]*)<\\/a>.*?<\\/td>", RegexOptions.Singleline);
                Regex regexDestination = new Regex("<td class=\"terminus\">([a-zA-Z .\\-_0-9]*)<\\/td>", RegexOptions.Singleline);
                Regex regexPassingTime = new Regex("<td class=\"passing_time\">((([0-9]{2}):([0-9]{2}))|([a-zA-Z à']*?))<\\/td>", RegexOptions.Singleline);

                Match matchName = regexName.Match(tr);
                Match matchPassingTime = regexPassingTime.Match(tr);
                Match matchDestination = regexDestination.Match(tr);

                if (matchPassingTime.Groups[5].Value == "")
                {
                    int trainArrivalHour = int.Parse(matchPassingTime.Groups[3].Value);
                    int trainArrivalMinute = int.Parse(matchPassingTime.Groups[4].Value);

                    DateTime now = DateTime.Now;
                    DateTime trainArrival = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, trainArrivalHour, trainArrivalMinute, 0);

                    if (trainArrivalHour < now.Hour)
                        trainArrival.AddDays(1);

                    Entries[i - 1].Find("HeurePassage").GetComponent<Text>().text = String.Format("{0:00}", trainArrival.Hour) + ":" + String.Format("{0:00}", trainArrival.Minute);

                    _trainSchedules.Add(trainArrival);
                }
                else
                {
                    Entries[i - 1].Find("HeurePassage").GetComponent<Text>().text = matchPassingTime.Groups[5].Value;
                    DateTime trainArrival = DateTime.Now;
                    _trainSchedules.Add(trainArrival);
                }

                Entries[i - 1].Find("Name").GetComponent<Text>().text = matchName.Groups[1].Value;
                Entries[i - 1].Find("Destination").GetComponent<Text>().text = matchDestination.Groups[1].Value;
            }

            GameObject.Find("Canvas/Footer/Date").GetComponent<Text>().text = String.Format("{0:00}", DateTime.Now.Hour) + ":" + String.Format("{0:00}", DateTime.Now.Minute) + ":" + String.Format("{0:00}", DateTime.Now.Second);
        }

        _updating = false;

        StopAllCoroutines();
        StartCoroutine(WaitAndUpdateSchedules(30));
    }

    IEnumerator WaitAndUpdateSchedules(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if(_updating == false)
            StartCoroutine(UpdateSchedules());
    }

    void Update()
    {
        if(_trainSchedules != null && _trainSchedules.Count > 0)
        {
            GameObject.Find("Canvas/Footer/Date/Updating").SetActive(_updating);

            for (int i = 0; i < _trainSchedules.Count; i++)
            {
                TimeSpan dt = _trainSchedules[i].Subtract(DateTime.Now);

                Text text = Entries[i].Find("HeureDepart").GetComponent<Text>();
                text.text = String.Format("{0:00}", dt.Minutes) + ":" + String.Format("{0:00}", dt.Seconds);

                if (dt.TotalSeconds < DeadTime)
                    text.color = DeadColor;
                else if (dt.TotalSeconds < RushTime)
                    text.color = RushColor;
                else
                    text.color = CoolColor;

                if(dt.TotalSeconds < 1)
                {
                    text.GetComponent<Text>().text = "00:00";

                    if (_updating == false && (DateTime.Now - _lastAutoUpdate).TotalSeconds > 10f)
                    {
                        _lastAutoUpdate = DateTime.Now;
                        StartCoroutine(UpdateSchedules());
                    }
                }
            }
        }
    }

}
