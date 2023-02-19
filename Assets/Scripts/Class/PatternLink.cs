using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternLink
{
    public string pattern;
    public int num;
    public PatternLink advanced;
    public bool isAdvanced;
    public bool isAdvancedFirstUse;
    public bool isRangedAttack;
    public float advancedUsage;
    

    public PatternLink(string c, int i, bool isRanged = false, bool Advanced = false, float advancedPercentage = 0.7f){
        pattern = c;
        num = i;
        advanced = null;
        isRangedAttack = isRanged;
        isAdvanced = Advanced;
        isAdvancedFirstUse = false;
        advancedUsage = advancedPercentage;
    }
    public PatternLink(string c, int i, float advancedPercentage){
        pattern = c;
        num = i;
        advanced = null;
        isAdvanced = false;
        isAdvancedFirstUse = false;
        isRangedAttack = false;
        advancedUsage = advancedPercentage;
    }

    public void AddAdvanced(PatternLink pl){
        advanced = pl;
    }
}
