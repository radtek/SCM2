using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCM;

public class QuestionnaireUI : UIBase
{
    public Transform Content;
    public GameObject ChoiceItem;
    public GameObject ChoiceAItem;
    public GameObject QaItem;
    public Text Title;

    public GameObject BuildChoiceItem()
    {
        var go = Instantiate(ChoiceItem) as GameObject;

        go.SetActive(true);
        go.transform.SetParent(Content);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        return go;
    }

    public GameObject BuildQaItem()
    {
        var go = Instantiate(QaItem) as GameObject;

        go.SetActive(true);
        go.transform.SetParent(Content);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        return go;
    }

    public GameObject BuildChoiceAItem(Transform parent)
    {
        var go = Instantiate(ChoiceAItem) as GameObject;

        go.SetActive(true);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        return go;
    }

    public void ClearContent()
    {
        foreach (Transform para in Content)
            Destroy(para.gameObject);
    }

    public void OnGetQuestionnaire(string qName)
    {
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Request2Srv("GetQuestionnaire", (data) =>
        {
            var isNew = data.ReadBool();

            if (!isNew)
            {
//                UIManager.Instance.Tips.AddTip("你已经答过此卷了！");
                return;
            }

            var isExists = data.ReadBool();

            if (!isExists)
            {
                return;
            }

            var q = new QuestionnaireInfo();

            q.Id = data.ReadString();

            while (data.Available > 0)
            {
                var question = data.ReadString();
                var aList = new List<string>();

                var acnt = data.ReadInt();

                if (acnt != 0)
                {
                    for (int j = 0; j < acnt; j++)
                    {
                        var answer = data.ReadString();
                        aList.Add(answer);
                    }
                }

                q.Questions[question] = aList;
            }

            ShowQuestionnaireInfo(q);
        });

        buff.Write(qName);
        conn.End(buff);
    }

    private void ShowQuestionnaireInfo(QuestionnaireInfo q)
    {
        gameObject.SetActive(true);
        ClearContent();

        Title.text = "问卷调查";

        for (int i = 0; i < q.Questions.KeyArray.Length; i++)
        {
            if (q.Questions [q.Questions.KeyArray [i]].Count == 0)
            {
                var qaItem = BuildQaItem();
                qaItem.transform.Find("Q").GetComponent<Text>().text = q.Questions.KeyArray [i];
            }
            else
            {
                var choiceItem = BuildChoiceItem();
               
                choiceItem.transform.Find("Q").GetComponent<Text>().text = q.Questions.KeyArray [i];

                for (int j = 0; j < q.Questions [q.Questions.KeyArray [i]].Count; j++)
                {
                    var a = BuildChoiceAItem(choiceItem.transform.Find("As"));

                    a.GetComponent<Toggle>().group = choiceItem.transform.Find("As").GetComponent<ToggleGroup>();
                    a.transform.Find("Txt").GetComponent<Text>().text = q.Questions [q.Questions.KeyArray [i]] [j];
                }
            }
        }

        qn = q;

        StartCoroutine(Refresh());
    }

    QuestionnaireInfo qn;

    public void OnSubmitQuestionnaireResult()
    {
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Send2Srv("SubmitQuestionnaireResult");

        var count = Content.childCount;

        buff.Write(qn.Id);

        for (int i = 0; i < count; i++)
        {
            var item = Content.GetChild(i);

            if (item.name == "ChoiceItem(Clone)")
            {
                bool hasChoice = false;
                var acnt = item.Find("As").childCount;

                for (int j = 0; j < acnt; j++)
                {
                    var aItem = item.Find("As").GetChild(j);

                    if (aItem.GetComponent<Toggle>().isOn)
                    {
                        buff.Write(aItem.Find("Txt").GetComponent<Text>().text);
                        hasChoice = true;
                    }
                }

                if (!hasChoice)
                    buff.Write("");
            }
            else if (item.name == "QA(Clone)")
            {
                buff.Write(item.Find("A").Find("Txt").GetComponent<Text>().text);
            }
        }

        conn.End(buff);

        gameObject.SetActive(false);
    }

    private IEnumerator Refresh()
    {
        yield return new WaitForEndOfFrame();
        var csf = Content.GetComponent<ContentSizeFitter>();
        csf.enabled = false;
        yield return new WaitForEndOfFrame();
        csf.enabled = true;
    }
}
