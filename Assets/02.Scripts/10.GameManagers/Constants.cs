using Unity.VisualScripting;

public static class Constants
{
    public static class Player
    {
        public const string Userid = "userId";
        public const string UsernameKey = "username";
        public const string TeamName = "teamName";
        public const string CountryName = "countryName";
        public const string FirstTimeKey = "firstTime";
        public const string PlayerHeadKey = "Player_Head";
        public const string PlayerFaceKey = "Player_Face";
        public const string PlayerFaceHairKey = "Player_Face_Hair";
        public const string PlayerHand = "Player_Hand";
        public const string PlayerHat = "Player_Hat";
        public const string PlayerEyeLeftKey = "Player_EyeLeft";
        public const string PlayerEyeRightKey = "Player_EyeRight";
        public const string PlayerUpperBodyKey = "Player_UpperBody";
        public const string PlayerLowerBodyKey = "Player_LowerBody";
        public const string PlayerHelmetKey = "Player_Helmet";
    }
    public static class  Horse
    {
        public const string HorseNameKey = "horseName";
        public const string HorseData = "Horse";
        public const string HorseBodyKey = "HorseBody";
        public const string HorseEyesKey = "HorseEyes";
        public const string HorseManeKey = "HorseMane";
        public const string HorseTailKey = "HorseTail";
        public const string HorseReinsKey = "HorseRein";
        public const string HorseSaddleKey = "HorseSaddle";
        public const string HorseReinsHeadKey = "HorseReinsHead";
        public const string HorseArmorKey = "HorseArmor";

    }
    public static class GameSettings
    {
        public const string MusicVolumeKey = "MusicVolume";
        public const string SFXVolumeKey = "SFXVolume";
        public const string LanguageKey = "language";
        public const string GraphicsQualityKey = "GraphicsQuality";
    }
    public static class  Environment
    {
        public const string Utov = "Utov";
    }
    public static class Prizes
    {
        public const string Money = "money";
        public const string Sheep = "sheep";
        public const string Horse = "horse";
        public const string Carpet = "carpet";
        public const string Camel = "camel";
        public const string Goat = "goat";
        public const string Bugdoy = "bugdoy";
        public const string Arpa = "arpa";
        public const string Water = "water";
        public const string StaminWater = "staminWater";
        public const string Apple = "apple";
        public const string Cow = "cow";
    }
    public static class Tutorial
    {
        public const string GamePlay = "GamePlayTutorial";
    }
    public static class Initialize
    {
        public const string skippAppear= "skippAppear";
    }
    public static class Timer
    {
        public const string LastUpdateTime = "lastupdatetime";
    }

    public static class Record
    {
        public const string BaxmalRacing = "baxmal";
        public const string Zarafshan = "zarafshan";
        public const string Egypt = "egypt";
        public const string Registon = "registon";
        public const string JomboyKopkari = "jomboyk";
    }
    public static class HorseCondition
    {
        public const string Power = "power";
        public const string Cooling = "cooling";
        public const string Stamina = "stamin";
        public const string Level = "horse1";
    }
    public static class HorseFoods
    {
        public const string Wheat = "wheatFood";
        public const string Barley = "barleyFood";
        public const string Apple = "appleFood";
        public const string Water = "waterFood";
        public const string StaminWater = "staminWater";
    }
    public static class Coins
    {
        public const string Nyufiy = "nyufiy";
        public const string Coin = "coin";
    }
    public static class PlayerItems
    {
        public const string Defense = "defense";
        public const string SlowDown = "slowdown";
        public const string WebSnare = "websnare";
        public const string Whip = "whip";
        public const string Horsedust = "horsedust";
    }
    public static class DailyPrizes
    {
        public const string PREF_STREAK_DAY = "DR_STREAK_DAY";
        public const string PREF_MONTH_PROGRESS = "DR_MONTH_PROGRESS";
        public const string PREF_LAST_CLAIM_DATE = "DR_LAST_CLAIM_DATE";
        public const string PREF_LAST_CLAIMED_CYCLE_DAY = "DR_LAST_CLAIMED_CYCLE_DAY";

        // Today reward (RewardDayUI save qiladi, DailyRewardUI claimda o¡®qiydi)
        public const string PREF_TODAY_REWARD_TYPE = "DR_TODAY_TYPE";
        public const string PREF_TODAY_REWARD_ENUM = "DR_TODAY_ENUM";
        public const string PREF_TODAY_REWARD_AMOUNT = "DR_TODAY_AMOUNT";
        public const string PREF_TODAY_REWARD_LANG_ID = "DR_TODAY_LANG";

    }
    public static class MapNames
    {
        public const string Zarafshan = "zarafmap";
        public const string Registan = "regismap";
        public const string Egypt = "egyptmap";
        public const string Japan = "japanmap";
        public const string PastDargom = "pastmap";
        public const string Chiroqchi = "chiroqmap";
    }
    public static class HomeEnivronments
    {
        public const string SelectedEnvironment = "selectedEnv";
    }
    public static class ZarafshanMapLayers 
    {
        public const string GrassLayer = "grassLayer";
        public const string DryGrassLayer = "drygrassLayer";
        public const string MudLayer = "mudLayer";
        public const string CliffLayer = "cliffLayer";
    }
    public static class UISounds 
    {
        public const string Click = "Click";
        public const string Confirm = "Confirm";
        public const string Error = "Error";
        public const string Success = "Success";
        public const string PopupClose = "PopupClose";
        public const string PopupOpen = "PopupOpen";
    }
    public static class RoomSound 
    {
        public const string HomeRoomSound = "HomeSound";
        public const string IntroSound = "IntroSound";
        public const string RacingSound = "RacingSound";
    }
    public static class VideoClips
    {
        public const string IntroVideo = "IntroVideo";
    }
    public static class HorseStaticMeshes
    {
        public const string Eyes = "eyes_default";
        public const string Reins = "reins_default";
        public const string HeadReins = "headReins_default";
        public const string Tail = "tail_default";
    }
    public static class HorseStaticMaterials
    {
        public const string Eyes = "eyesMaterial_default";
    }

    public static class HorseFoodAmount
    {
        public const int Water = 6;
    }
}
