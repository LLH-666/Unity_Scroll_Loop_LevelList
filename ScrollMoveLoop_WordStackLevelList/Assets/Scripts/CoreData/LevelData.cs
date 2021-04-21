using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelData
{
    public int Lv;
    public List<string> Words = new List<string>();
    public string Hint;

    private int maxWordLength = -1;

    public int MaxWordLength
    {
        get
        {
            if (maxWordLength == -1) SetMaxWordLength();
            return maxWordLength;
        }
    }

    public string LevelInfo
    {
        get { return "Lv_" + Lv; }
    }

    /// <summary>
    /// Checks if the given word is a word in this level
    /// </summary>
    public bool IsWordInLevel(string word)
    {
        for (int i = 0; i < Words.Count; i++)
        {
            if (word == Words[i])
            {
                return true;
            }
        }

        return false;
    }

    private void SetMaxWordLength()
    {
        maxWordLength = int.MinValue;

        for (int i = 0; i < Words.Count; i++)
        {
            maxWordLength = Mathf.Max(maxWordLength, Words[i].Length);
        }
    }
}