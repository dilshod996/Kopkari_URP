using System.Collections.Generic;

[System.Serializable]
public class Translation
{
    public int Id;
    public string english;
    public string russian;
    public string uzbek;
    public string kazak;
}

[System.Serializable]
public class TranslationList
{
    public List<Translation> translations;
}
