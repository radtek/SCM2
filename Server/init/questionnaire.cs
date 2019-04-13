void Init(GameServer srv)
{
	var qs = new StableDictionary<string, Questionnaire>();

	// 问卷1
	var q1 = new Questionnaire();

	var subject11 = "1.您从何处得知我们的游戏并参与到此次的测试中？";
	var answers11 = new List<string>();
	answers11.Add("TapTap网页或APP");
	answers11.Add("经由玩过的朋友介绍");
	answers11.Add("主动寻找了该类游戏");

	var subject12 = "2.您对RTS（即时战略）类型游戏的喜爱程度如何？";
	var answers12 = new List<string>();
	answers12.Add("骨灰级玩家，非常喜欢");
	answers12.Add("比较喜欢");
	answers12.Add("一般，无感");
	answers12.Add("不太喜欢");
	answers12.Add("非常不喜欢");

	var subject13 = "3.在一局游戏后，你对游戏核心玩法(不考虑画面)的初次印象如何？";
	var answers13 = new List<string>();
	answers13.Add("非常满意");
	answers13.Add("比较满意");
	answers13.Add("一般，无感");
	answers13.Add("比较不满");
	answers13.Add("非常不满");

	q1.Info.Id = "1";
	q1.Info.Questions[subject11] = answers11;
	q1.Info.Questions[subject12] = answers12;
	q1.Info.Questions[subject13] = answers13;

	qs[q1.Info.Id] = q1;


	// 问卷2
	var q2 = new Questionnaire();

	var subject21 = "1.目前您对游戏核心玩法（不考虑画面）的整体满意度如何？";
	var answers21 = new List<string>();
	answers21.Add("非常满意");
	answers21.Add("比较满意");
	answers21.Add("一般，无感");
	answers21.Add("比较不满");
	answers21.Add("非常不满");

	var subject22 = "2.游戏玩法设计（不考虑画面的情况下）中的哪部分最令你满意？";
	var answers22 = new List<string>();
	answers22.Add("多类型科技与兵种");
	answers22.Add("战争迷雾与侦查");
	answers22.Add("多建筑暴兵");
	answers22.Add("经济与产能之间的平衡");
	answers22.Add("中立单位带来的随机性");

	var subject23 = "3.您对当前整局战斗所消耗的时间是否满意？";
	var answers23 = new List<string>();
	answers23.Add("满意");
	answers23.Add("过长");
	answers23.Add("过短");
	
	var subject24 = "4.对游戏有什么意见或建议：";
	var answers24 = new List<string>();
	
	var subject25 = "5.您的QQ号为：";
	var answers25 = new List<string>();
	
	var subject26 = "6.电话号码为：";
	var answers26 = new List<string>();
	

	q2.Info.Id = "2";
	q2.Info.Questions[subject21] = answers21;
	q2.Info.Questions[subject22] = answers22;
	q2.Info.Questions[subject23] = answers23;
	q2.Info.Questions[subject24] = answers24;
	q2.Info.Questions[subject25] = answers25;
	q2.Info.Questions[subject26] = answers26;

	qs[q2.Info.Id] = q2;

	QuestionnaireMgr.Questionnaires = qs;
}