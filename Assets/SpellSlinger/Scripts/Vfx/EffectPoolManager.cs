using System.Collections.Generic;
using UnityEngine;

public class EffectPoolManager : MonoBehaviour
{
    public static EffectPoolManager instance;
    public GameObject effectPrefab;
    public int poolSize = 20;
    private List<GameObject> _pooledEffects;

    void Awake()
    {
        instance = this;
        _pooledEffects = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(effectPrefab);
            obj.SetActive(false);
            _pooledEffects.Add(obj);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < _pooledEffects.Count; i++)
        {
            if (!_pooledEffects[i].activeInHierarchy)
            {
                return _pooledEffects[i];
            }
        }

        GameObject newObj = Instantiate(effectPrefab);
        newObj.SetActive(false);
        _pooledEffects.Add(newObj);
        return newObj;
    }

    public void PlayOrganEffect(Vector3 position)
    {
        GameObject effect = GetPooledObject();

        if (effect != null)
        {
            effect.transform.position = position;
            effect.SetActive(true);
        }
    }
}
