using System;
using System.Collections.Generic;
using UnityEngine;

// 프레임 정리 1단계 산출물(순수 데이터, Editor 의존성 없음):
// 스프라이트시트에서 오브젝트별로 묶인 프레임 목록 하나를 담는다.
// 2단계(Anim State / 프리팹 생성) 스크립트는 이 데이터만 읽어서 동작하도록
// 슬라이싱 로직과 완전히 분리해뒀다.
[CreateAssetMenu(fileName = "SpriteFrameSet", menuName = "Dungeon/Sprite Frame Set")]
public class SpriteFrameSet : ScriptableObject
{
    [Serializable]
    public class StateFrames
    {
        // "idle" / "run" / "hit" / "open" / "static" 등. 프레임 접미사가 없는
        // 단일 스프라이트(floor_1, wall_mid 등)는 "static" 하나만 가진다.
        public string stateName;
        public List<Sprite> frames = new List<Sprite>();
    }

    public string objectName;
    public List<StateFrames> states = new List<StateFrames>();
}
