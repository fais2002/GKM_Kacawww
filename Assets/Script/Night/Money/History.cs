using UnityEngine;

[System.Serializable]
public class History
{
    public enum Type { Incomes, Expenses}
    public Type type;
    public string describe;
    public float amount;
    public float balance;
    public string time;
}
