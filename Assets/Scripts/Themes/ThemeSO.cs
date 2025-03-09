using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTheme", menuName = "Scriptable Objects/Theme")]
public class ThemeSO : ScriptableObject {
    public Sprite bgStart;
    public AudioClip bgmStart;
    public Sprite bgGame;
    public AudioClip bgmGame;
}
