using System;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public event Action<float> OnMentalChanged;
    private Stats stats; //캐릭터 정보
    public Entity target; //공격 대상

    // 체력(HP) 프로퍼티 : 0 ~ maxHP 사이의 값을 넘어갈 수 없도록 설정
    public float HP
    {
        set => stats.HP = Mathf.Clamp(value, 0, maxHP);
        get => stats.HP;
    } 
    // 마나(MP) 프로퍼티 : 0 ~ maxMP 사이의 값을 넘어갈 수 없도록 설정
    public float MP
    {
        set => stats.MP = Mathf.Clamp(value, 0, maxMP);
        get => stats.MP;
    }

    public float Mental
    {
        //set => stats.Mental = Mathf.Clamp(value, 0, maxMental);
        set
        {
            float previousValue = stats.Mental;
            stats.Mental = Mathf.Clamp(value, 0, maxMental);
            if(previousValue != stats.Mental){
                OnMentalChanged?.Invoke(stats.Mental);
            }
        }
        get => stats.Mental;
    }

    // public float Stamina
    // {
    //     set => stats.Stamina = Mathf.Clamp(value, 0, maxStamina);
    //     get => stats.Stamina;
    // }

    public float Speed
    {
        set => stats.Speed = Mathf.Max(0, value);
        get => stats.Speed;
    }

    public float Attack_Power
    {
        set => stats.AttacK_Power = Mathf.Max(0, value);
        get => stats.AttacK_Power;
    }


    public abstract float maxHP { get; }
    public abstract float maxMP { get; }
    public abstract float maxMental { get; }
    //public abstract float maxStamina { get; }

    protected void Setup()
    {
        HP = maxHP;
        MP = maxMP; //오타 수정 3/28
        Mental = maxMental; // 각자의 최대치로 초기화 3/28, 기존의 시작값 70은 플레이어 컨트롤러에서 따로 처리 요망
        //Stamina = maxStamina;
    }



    //상대방을 공격할 때 상대방의 TakeDamage() 호출
    // 매개변수 damage는 공격하는 본인 공격력
    public abstract void TakeDamage(float damage);

    [System.Serializable]
    public struct Stats
    {   //이름, 레벨, 스탯, 체력/마나, 정신력 등의 캐릭터 수치
        [HideInInspector]
        public float HP;
        [HideInInspector]
        public float MP;
        [HideInInspector]
        public float Mental;
        //[HideInInspector]
        //public float Stamina;
        [HideInInspector]
        public float AttacK_Power;
        [HideInInspector]
        public float Speed;
    }
}


//잘 수정됬는지 확인부탁함