using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class boss1PatternExtracter : MonoBehaviour
{
    public List<PatternLink> patternList;
    public Boss1Behaviour b1b;
    public int lastPatternNum;
    public bool isAdvanced;

    public List<PatternLink> patternList_N;
    public List<PatternLink> closePatternList_N;
    public List<PatternLink> rangePatternList_N;
    public List<PatternLink> patternList_A;
    public List<PatternLink> closePatternList_A;
    public List<PatternLink> rangePatternList_A;


    public void StartPatternList(Boss1Behaviour bb){
        patternList = new List<PatternLink>();
        b1b = bb;
        lastPatternNum = -1;
    }

    public void PreparePatterns(){
        patternList_N = patternList.Where( x => !x.isAdvanced ).ToList();
        closePatternList_N = patternList.Where( x => !x.isAdvanced && !x.isRangedAttack).ToList();
        rangePatternList_N  = patternList.Where( x => !x.isAdvanced && x.isRangedAttack).ToList();; 
        patternList_A = patternList.ToList();
        closePatternList_A = patternList.Where( x => x.isRangedAttack).ToList();
        rangePatternList_A = patternList.Where( x => x.isRangedAttack).ToList();
    }

    public void Advanced(){
        isAdvanced = true;
    }

    //뽑아주는 걸로 사용
    public string NextPattern(out int res) {
        if(isAdvanced) {
            return RandomExtract(patternList_A, out res);
        }
        else{
            return RandomExtract(patternList_N, out res);
        }
        
    }


    //가드 후 쓸 수 있는 짧은 패턴들 중 반환
    public string ClosePattern(out int res) {
        if(isAdvanced) {
            return RandomExtract(closePatternList_A, out res);
        }
        else{
            return RandomExtract(closePatternList_N, out res);
        }
    }

    //거리가 있는 패턴들 중 반환
    public string RangePattern(out int res){
        if(isAdvanced) {
            return RandomExtract(rangePatternList_A, out res);
        }
        else{
            return RandomExtract(rangePatternList_N, out res);
        }
    }

    public string RandomExtract(List<PatternLink> pl, out int res){
        res = 1;
        return "Pattern1";
        /*
        do {
            res = Random.Range(0, pl.Count);
        } while(res == lastPatternNum);
        
        PatternLink ret = pl[res];
        lastPatternNum = res;
        if(isAdvanced && pl[res].advanced is not null){
            if(! (pl[res].advanced.isAdvancedFirstUse)){
                ret = pl[res].advanced;
                pl[res].advanced.isAdvancedFirstUse = true;
                res = pl[res].advanced.num;
            }
            else if(Random.Range(0,1) < pl[res].advanced.advancedUsage ){
                ret = pl[res].advanced;
                res = pl[res].advanced.num;
            }
        }
        Debug.Log(res);
        return ret.pattern;

        */
    }
}
