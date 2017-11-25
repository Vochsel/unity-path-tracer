// -- Utils

inline float clamp(float x) { return x < 0.0f ? 0.0f : x > 1.0f ? 1.0f : x; }

inline int toInt(float x) { return int(pow(clamp(x), 1 / 2.2) * 255 + .5); }  // convert RGB float in range [0,1] to int in range [0, 255] and perform gamma correction

// -- Intersections

/**
	Intersect Ray with Sphere
*/
float intersect_sphere(ptObject sph, inout Ray r, inout float depth, inout ptHit a_hit)
{
	// Extract position from transform
	float3 pos = sph.transform._m03_m13_m23;

	// Extract scale from transform
	float3 scale = float3(length(sph.transform._m00_m01_m02), length(sph.transform._m10_m11_m12), length(sph.transform._m20_m21_m22));
	float rad = length(scale) * 0.5f;
	rad = length(sph.transform._m00_m01_m02) / 2.0f;

	// Vector from position to ray origin
	float3 op = pos - r.orig;
	float t;

	// Quadratic B value
	float b = dot(op, r.dir);

	// Discriminant
	float disc = b*b - dot(op, op) + rad*rad;

	// Return if complex
	if (disc<0) return 0;
	else disc = sqrt(disc);
	depth = disc * 2.0f;
	
	// Calculate hit normal
	float3 x = r.orig + r.dir*(b - disc);
	a_hit.position = x;
	a_hit.normal = normalize(x - pos);

	// Return closest point
	return (t = b - disc)>EPSILON ? t : ((t = b + disc)>EPSILON ? t : 0);
}

/**
	Intersect Ray with Plane
*/
float intersect_plane(ptObject plane, inout Ray r, inout float t, inout ptHit a_hit)
{
	float3 pos = plane.transform._m03_m13_m23;
	float3 dim = float3(length(plane.transform._m00_m01_m02), length(plane.transform._m10_m11_m12), length(plane.transform._m20_m21_m22));
	
	float3 n	= normalize(mul((plane.transform), float4(0., -1., 0., 0.))).xyz;
	float3 fwd	= normalize(mul((plane.transform), float4(1, 0., 0., 0.))).xyz;
	float3 rgt	= normalize(mul((plane.transform), float4(0., 0., 1, 0.))).xyz;
	float3 dd = dim;
	a_hit.normal = n;

	float denom = dot(n, r.dir); 
    if (denom > 1e-6) { 
        float3 p0l0 = pos - r.orig; 
        t = dot(p0l0, n) / denom; 
	
		float3 hpos = r.orig + r.dir * t; 
		a_hit.position = hpos;
		float3 hdist = ((hpos - pos));
	
		float3 lhdist = mul(plane.invTransform, float4(hdist.x, hdist.y, hdist.z, 0.0)).xyz;

		float ll = 5.0;
		float l1 = length(fwd) * 5;
		float l2 = length(rgt) * 5;

		float s1 = dot(hdist, fwd);
		float s2 = dot(hdist, rgt);
	
		if(lhdist.x > -ll && lhdist.x < ll)		
				if(lhdist.z > -ll && lhdist.z < ll)	
					return (t)>EPSILON ? t : ((t)>EPSILON ? t : 0); 
    } 

	return 0.0f;
}

/**
	Intersect Ray with Box
	 - Not implemented
*/
float intersect_box(ptObject box, inout Ray r, inout float depth, inout ptHit a_hit)
{
	float3 pos = box.transform._m03_m13_m23;
	float3 bounds = float3(box.transform._m00, box.transform._m11, box.transform._m22);
	a_hit.normal = float3(0., 0., 0.);
	return 0.0f;
}