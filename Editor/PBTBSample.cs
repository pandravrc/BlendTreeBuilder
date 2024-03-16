using UnityEngine;
using UnityEditor;
public class PBTBSample : MonoBehaviour
{
    [MenuItem("PBTB/Gen_PBTBSample")]
    static void createDBT()
    {
        var bb = new PBlendTreeBuilder("Assets/Pan/PBTBSample");
        bb.rootDBT(() => {
            bb.add1D("1", "Active", () => {
                bb.addMotion(0f, $@"ActiveX");
                bb.addMotion(1f, $@"ActiveO");
                bb.add1D(1f, "Color", () => {
                    bb.currentTree().useAutomaticThresholds = true;
                    bb.addMotion(0f, $@"ColorR");
                    bb.addMotion(1f, $@"ColorG");
                    bb.addMotion(2f, $@"ColorB");
                });
                bb.add1D(1f, "PosX", () => {
                    bb.addMotion(-1f, $@"PosX-1");
                    bb.addMotion(1f, $@"PosX+1");
                });
                bb.add1D(1f, "PosZ", () => {
                    bb.addMotion(-1f, $@"PosZ-1");
                    bb.addMotion(1f, $@"PosZ+1");
                });
                bb.add1D(1f, "BitControll", () => {
                    for (int i = 0; i < 4; i++)
                    {
                        int D1 = i % 2;
                        int D2 = (i - D1) / 2;
                        bb.addMotion(i, $@"Bit{D2}{D1}");
                    }
                });
            });
        });
        bb.animatorMake();
    }
}