using System.Collections.Generic;

namespace VocalKnight.Components
{
    internal static class WordBase
    {
        private static HashSet<string> _words = new HashSet<string>()
        {
            "weed","art","honor","ideal","answer","explode","protect","trust","vocal","action",
            "see","harm","hope","glove","solo","movie","shell","give","quit","positive","train",
            "minute","second","hour","store","rock","rebel","drop","dive","claw","wing","red","blue",
            "green","yellow","orange","pink","white","black","night","knight","bed","champion",
            "studio","sock","genuine","temporary","business","grass","thought","relative","lion",
            "fish","test","exam","midterm","school","class","low","high","down","up","left","right",
            "instinct","parameter","capable","incapable","meter","reliable","old","young","age","pest",
            "goat","react","soft","hard","game","desert","dessert","cake","eat","drink","soda","pop",
            "sweet","sour","meat","veggie","cereal","tomato","potato","fright","fear","mansion",
            "wyoming","tip","toe","window","vision","glasses","distaster","insurance","promotion",
            "spike","point","laser","peek","peak","bee","new","exist","fist","foot","hand","neck","head",
            "quote","fax","thirsty","height","flood","depth","width","uncertain","certain","mature",
            "jelly","party","god","gamer","academy","fire","nightmare","fire","horse","speaker","speak",
            "cruel","unfair","fair","spill","shelf","life","death","die","dye","dying","family",
            "trust","depend","combine","rent","riot","culture","pledge","crop","wire","eye","glow",
            "admire","beg","ghost","host","shot","nut","nothing","none","monk","enjoy","equation","storm",
            "enjoy","fade","fading","short","tall","fame","famous","accept","ally","enemy","fight",
            "love","war","classify","item","bomb","will","matt","john","jacob","jenny","jaime","juan",
            "anthony","tony","kevin","keith","object","alex","craig","logan","brian","megan","kyle","ryan",
            "balance","feel","feeling","eel","emotion","crisis","order","nervous","angry","anger",
            "hunger","hungry","laundry","cook","cooking","deep","nest","mantis","husk","fly","liberty",
            "mention","wife","husband","partner","sale","glare","decide","not","bang","sunrise","sun",
            "sunset","resign","available","grant","chris","christina","katie","room","bedroom","mend",
            "bug","insect","beetle","link","hollow","illusion","scandal","gesture","approval","word",
            "sentence","period","question","command","effect","affect","friend","society","among",
            "fort","gear","air","water","wet","slip","slippery","earth","ground","steel","iron","gold",
            "silver","sit","stand","sandal","shoe","hobby","job","leaf","tree","bush","coffee","tea",
            "food","credible","incredible","fantastic","wonderful","great","good","bad","better",
            "worse","best","worst","hotdog","burger","zoom","boom","strap","angle","national","usa",
            "canada","china","russia","india","australia","japan","turkey","africa","brazil",
            "thanksgiving","wed","mexico","spain","france","french","american","america","bridge",
            "person","people","peep","hole","hold","halt","tear","bear","swear","share","care","hair",
            "turtle","tortoise","rune","escape","pair","pants","scissors","apple","micro","soft","hard",
            "cow","pig","chicken","swivel","turn","great","rig","car","skateboard","bus","plane",
            "airplane","skate","skating","cat","dog","hamster","hunt"
        };

        public static string[] GetWordBase()
        {
            string[] wordlist = new string[_words.Count];
            _words.CopyTo(wordlist, 0);
            return wordlist;
        }
    }
}
