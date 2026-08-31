using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class uses unity's particle emitter to draw the particles in a particle system
/// It is designed to be a seperate modular element so you may implement your own drawer to suit your particular game</summary>
public class LPDrawParticleSystem : MonoBehaviour 
{	
	private const float ParticleLifetime = 99999f;

	[Tooltip("How big should the particles appear relative to their size in the simulation")]	
	public float ParticleDrawScale = 4f;
	[Tooltip("液滴渲染所在的世界 Z 深度。LiquidFun 物理仍然只使用 X/Y 坐标")]
	public float RenderPlaneZ = 0f;
	[Tooltip("是否自动跟随所属 LiquidParticleSystem 的世界 Z 深度")]
	public bool FollowParticleSystemZ = true;
	[Tooltip("This drawer will draw particles in its parent particle system with the same userdata value as this")]
	public int DrawParticlesWithThisUserData = 0;
	protected ParticleSystem.Particle[] particles = new ParticleSystem.Particle[0];
	protected LPParticleSystem sys;
	private Transform particleSystemTransform;
	public bool debug;
	private ParticleSystem particleSystemObj;
	
	public void Initialise(LPParticleSystem partsys)
	{
		particleSystemTransform = partsys.transform;
		particleSystemObj = GetComponent<ParticleSystem>();
		if (particleSystemObj == null)
		{
			particleSystemObj = gameObject.AddComponent<ParticleSystem>();
		}

		ParticleSystem.MainModule main = particleSystemObj.main;
		main.startSize = partsys.ParticleRadius * ParticleDrawScale;
		main.startLifetime = ParticleLifetime;
		main.startSpeed = 0f;
		main.simulationSpace = ParticleSystemSimulationSpace.World;

		ParticleSystem.EmissionModule emission = particleSystemObj.emission;
		emission.enabled = false;
	}
	
	/// <summary>
	/// Redraw the particles in the particle system</summary>
	/// <param name="partdata">An array of LPParticle structs, this is available in LPParticle system</param>
	public virtual void UpdateParticles(List<LPParticle> partdata)
	{	
		if (!PrepareParticles(partdata.Count)) return;
		
		if (debug && particles.Length > 2)
		{
			Debug.Log ( "part 0 "+ particles[0].rotation +" part 1 "+ particles[1].rotation +" part 2 "+ particles[2].rotation);
		}
	
		for (int i=0; i < partdata.Count; i ++)
		{		
			Vector3 particlePosition = partdata[i].Position;
			particlePosition.z = GetRenderPlaneZ();
			particles[i].position = particlePosition;
			particles[i].startColor = partdata[i]._Color;
		}
		
		ApplyParticles(partdata.Count);
	}

	/// <summary>
	/// 获取当前液滴应使用的世界 Z 深度。
	/// </summary>
	private float GetRenderPlaneZ()
	{
		return FollowParticleSystemZ && particleSystemTransform != null
			? particleSystemTransform.position.z
			: RenderPlaneZ;
	}

	/// <summary>
	/// 确保现代粒子系统拥有可写入指定数量液滴的缓冲区。
	/// </summary>
	protected bool PrepareParticles(int particleCount)
	{
		if (particleSystemObj == null)
		{
			Debug.LogError("[LiquidPhysics2D] ParticleSystem 绘制组件尚未初始化。", this);
			return false;
		}

		if (particleCount == 0)
		{
			particleSystemObj.Clear();
			return false;
		}

		if (particles.Length < particleCount)
		{
			particles = new ParticleSystem.Particle[particleCount];
		}

		ParticleSystem.MainModule main = particleSystemObj.main;
		if (main.maxParticles < particleCount)
		{
			main.maxParticles = particleCount;
		}

		int currentParticleCount = particleSystemObj.GetParticles(particles);
		if (currentParticleCount < particleCount)
		{
			particleSystemObj.Emit(particleCount - currentParticleCount);
			particleSystemObj.GetParticles(particles);
		}

		return true;
	}

	/// <summary>
	/// 将 LiquidFun 已更新的位置和颜色数据回写到现代粒子系统。
	/// </summary>
	protected void ApplyParticles(int particleCount)
	{
		particleSystemObj.SetParticles(particles, particleCount);
	}
	
	/// <summary>
	/// Redraw the particles in the particle system, but only ones with a certain userdata value</summary>
	/// <param name="partdata">An array of LPParticle structs, this is available in LPParticle system</param>
	public void UpdateParticles(List<LPParticle> allpartdata,bool multi)
	{
		List<LPParticle> partsforme = new List<LPParticle>();
		
		foreach (LPParticle part in allpartdata)
		{
			if (part.UserData == DrawParticlesWithThisUserData)
			{
				partsforme.Add(part);
			}
		}
		
		UpdateParticles(partsforme);
	}
}
