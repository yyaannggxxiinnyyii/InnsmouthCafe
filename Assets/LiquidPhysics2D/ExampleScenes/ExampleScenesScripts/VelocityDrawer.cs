using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VelocityDrawer : LPDrawParticleSystem
{
	public float Velscale = 1f;
	
	public override void UpdateParticles(List<LPParticle> partdata)
	{
		if (!PrepareParticles(partdata.Count)) return;
		
		for (int i=0; i < partdata.Count; i ++)
		{
			particles[i].position  = partdata[i].Position;
			particles[i].startColor = new Color(0.5f+(partdata[i].Velocity.x*0.3f),(partdata[i].Velocity.y*0.3f),0.5f);
		}
		
		ApplyParticles(partdata.Count);
	}
}
