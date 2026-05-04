using System.Collections.Generic;
using UnityEngine;

public static class SwarmManager
{
    private static Dictionary<string, EnemyController> elites = new Dictionary<string, EnemyController>();
    private static Dictionary<string, List<EnemyController>> allEnemies = new Dictionary<string, List<EnemyController>>();

    public static void RegisterEnemy(string type, EnemyController enemy)
    {
        if (!allEnemies.ContainsKey(type))
        {
            allEnemies[type] = new List<EnemyController>();
        }
        allEnemies[type].Add(enemy);

        if (!elites.ContainsKey(type) || elites[type] == null || !elites[type].gameObject.activeInHierarchy)
        {
            SetElite(type, enemy);
        }
    }

    public static void UnregisterEnemy(string type, EnemyController enemy)
    {
        if (allEnemies.ContainsKey(type))
        {
            allEnemies[type].Remove(enemy);
            if (elites.ContainsKey(type) && elites[type] == enemy)
            {
                // Choose a new elite
                if (allEnemies[type].Count > 0)
                {
                    // For simplicity, choose the first one. You can make it smarter, e.g., closest to player or random.
                    EnemyController newElite = allEnemies[type][0];
                    SetElite(type, newElite);
                }
                else
                {
                    elites.Remove(type);
                }
            }
        }
    }

    private static void SetElite(string type, EnemyController enemy)
    {
        // Reset old elite
        if (elites.ContainsKey(type) && elites[type] != null && elites[type] != enemy)
        {
            elites[type].isElite = false;
            if (elites[type].spriteRenderer != null)
            {
                elites[type].spriteRenderer.color = Color.white;
            }
        }

        elites[type] = enemy;
        enemy.isElite = true;
        if (enemy.spriteRenderer != null)
        {
            enemy.spriteRenderer.color = Color.magenta;
        }
        Debug.Log($"New elite for {type}: {enemy.gameObject.name}");
    }

    public static EnemyController GetElite(string type)
    {
        return elites.ContainsKey(type) ? elites[type] : null;
    }

    public static List<EnemyController> GetAllEnemies(string type)
    {
        return allEnemies.ContainsKey(type) ? new List<EnemyController>(allEnemies[type]) : new List<EnemyController>();
    }
}