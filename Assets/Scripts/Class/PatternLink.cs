using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternLink
{
    public string pattern;
    public PatternLink advanced;
    public bool isAdvanced;
    public bool isAdvancedFirstUse;
    public bool isRangedAttack;
    public float advancedUsage;
    

    public PatternLink(string c, bool isRanged = false, bool Advanced = false, float advancedPercentage = 0.7f){
        pattern = c;
        advanced = null;
        isRangedAttack = isRanged;
        isAdvanced = Advanced;
        isAdvancedFirstUse = false;
        advancedUsage = advancedPercentage;
    }
    public PatternLink(string c, float advancedPercentage){
        pattern = c;
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
