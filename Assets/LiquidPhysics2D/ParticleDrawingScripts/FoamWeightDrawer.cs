using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FoamWeightDrawer : LPDrawParticleSystem
{
	public Color Foam;
	public Color Liquid;
	
	public AnimationCurve curve;
		
	public override void UpdateParticles(List<LPParticle> partdata)
	{	
		if (!PrepareParticles(partdata.Count)) return;
		
		for (int i=0; i < partdata.Count; i ++)
		{		
			particles[i].position  = partdata[i].Position;				
			particles[i].startColor = Color.Lerp(Foam,Liquid,curve.Evaluate( partdata[i].Weight));
		}
		
		ApplyParticles(partdata.Count);
	}
}

