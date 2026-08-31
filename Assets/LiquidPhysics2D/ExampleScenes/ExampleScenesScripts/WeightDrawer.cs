using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeightDrawer : LPDrawParticleSystem
{
	public float Wscale = 1f;
	
	public override void UpdateParticles(List<LPParticle> partdata)
	{	
		if (!PrepareParticles(partdata.Count)) return;
		
		for (int i=0; i < partdata.Count; i ++)
		{		
			particles[i].position  = partdata[i].Position;
			float val =  partdata[i].Weight*Wscale;
			particles[i].startColor = new Color(1f-val,0f, val);
		}
		
		ApplyParticles(partdata.Count);
	}
}
