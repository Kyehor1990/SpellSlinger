using System;
using UnityEngine;

public class PooledEffect : MonoBehaviour
{
  private ParticleSystem _particleSystem;

  private void Awake()
  {
    _particleSystem = GetComponent<ParticleSystem>();
  }

  private void OnEnable()
  {
    _particleSystem.Play();
  }

  void Update()
  {
    if (!_particleSystem.IsAlive())
    {
      gameObject.SetActive(false);
    }
  }
}
