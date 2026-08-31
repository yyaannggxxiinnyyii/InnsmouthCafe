using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThreeColorFoamWeightDrawer : LPDrawParticleSystem
{
	public Color High;
	public Color Mid;
	public Color Low;
	
	public AnimationCurve curve;
	
	public float divisor = 5f;
	public float threshold = 0.8f;
		
	public override void UpdateParticles(List<LPParticle> partdata)
	{		
		if (!PrepareParticles(partdata.Count)) return;
		
		for (int i=0; i < partdata.Count; i ++)
		{		
			particles[i].position  = partdata[i].Position;

			float val = 1- ( curve.Evaluate(partdata[i].Weight/divisor));
				
			if (val < threshold)
			{
				particles[i].startColor = Color.Lerp(Low,Mid,val/threshold) ;
			}
			else
			{
				particles[i].startColor = Color.Lerp(Mid,High,(val-threshold)/(1f-threshold));
			}				
		}		
		ApplyParticles(partdata.Count);
	}
}
