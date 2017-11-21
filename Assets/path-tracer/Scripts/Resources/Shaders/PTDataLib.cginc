// Each #kernel tells which function to compile; you can have many kernels
#pragma once

/* ==== Ray Data ==== */

struct Ray {
	float3 orig;
	float3 dir;
};

Ray MakeRay(float3 a_origin, float3 a_dir) 
{
	Ray r;
	r.orig = a_origin;
	r.dir = a_dir;
	return r;
}

struct ptHit
{
	float3 normal;
};

/* ==== Light ==== */
struct ptLight
{
	float3 color;
    float intensity;
    float range;
    uint lightType;
    float4x4 worldMatrix;
};

/* ==== BVH ==== */

struct BVHNode
{
	uint lNode, rNode;
	uint tIndex, tCount;
};

/* ==== Render Settings ==== */

struct ptRenderSettings
{
    int outputWidth, outputHeight;
    int samples, bounces;
};

/* ==== Shapes ==== */

struct ptMaterial
{
    float4 albedo, emission;
    float metallic, smoothness;
};

struct ptObject
{
	ptMaterial material;
	float4x4 transform, invTransform;
	uint shapeType;
}; 

struct Sphere {
	float rad;
	float3 pos, emi, col;

	ptMaterial material;

	float intersect(inout Ray r, inout float depth)  {
		float3 op = pos - r.orig;    // distance from ray.orig to center sphere 
		float t, epsilon = 0.000000001f;  // epsilon required to prevent floating point precision artefacts
		float b = dot(op, r.dir);    // b in quadratic equation
		float disc = b*b - dot(op, op) + rad*rad;  // discriminant quadratic equation
		if (disc<0) return 0;       // if disc < 0, no real solution (we're not interested in complex roots) 
		else disc = sqrt(disc);    // if disc >= 0, check for solutions using negative and positive discriminant
		depth = disc * 2.0f;
		return (t = b - disc)>epsilon ? t : ((t = b + disc)>epsilon ? t : 0); // pick closest point in front of ray origin
	}
};

struct Triangle {
	float3 v0, v1, v2, normal;
	uint objIdx;
	

	float intersect(inout Ray r, inout float t) {

		    float3 v0v1 = v1 - v0; 
			float3 v0v2 = v2 - v0; 
			float3 pvec = cross(r.dir, v0v2); 
			float det = dot(v0v1, pvec); 
			// if the determinant is negative the triangle is backfacing
			// if the determinant is close to 0, the ray misses the triangle
			if (det < 0.001f) return false; 
	 
			float invDet = 1 / det; 
 
			float3 tvec = r.orig - v0; 
			float u = dot(tvec, pvec) * invDet; 
			if (u < 0 || u > 1) return false; 
 
			float3 qvec = cross(tvec,v0v1); 
			float v = dot(r.dir, qvec) * invDet; 
			if (v < 0 || u + v > 1) return false; 
 
			t = dot(v0v2, qvec) * invDet; 
 
			return true; 

	/*	float u = 0.0f;
		float v = 0.0f;

    
		float kEpsilon = 0.0001f;


		float3 v0v1 = v1 - v0; 
		float3 v0v2 = v2 - v0; 
		float3 pvec = cross(r.dir, v0v2); 
		float det = dot(v0v1, pvec);
		if (det < kEpsilon) return false; 
		if (abs(det) < kEpsilon) return false; 

		float3 cop = camToWorld._m03_m13_m23;
	

		float3 norig = r.orig;
		float3 ndir = r.dir;

		float3 nv0 = v0;
		float3 nv1 = v1;
		float3 nv2 = v2;


		//float3 v0v1 = (v1 - v0); 
		//float3 v0v2 = (v2 - v0); 
		// no need to normalize
		float3 N = cross(v0v1, v0v2); // N 
		float denom = dot(N,N); 
 
		// Step 1: finding P
 
		// check if ray and plane are parallel ?
		float NdotRayDirection = dot(N, ndir); 
		if (abs(NdotRayDirection) < kEpsilon) // almost 0 
			return false; // they are parallel so they don't intersect ! 
 
		// compute d parameter using equation 2
		float d = dot(N, nv0); 
 
		// compute t (equation 3)
		//This was the place -_-
		t = (dot(N, v0 - r.orig)) / NdotRayDirection; 
		// check if the triangle is in behind the ray
		if (t < 0) return false; // the triangle is behind 
 
		// compute the intersection point using equation 1
		float3 P = norig + t * ndir; 
 
		// Step 2: inside-outside test
		float3 C; // vector perpendicular to triangle's plane 
 
		// edge 0
		float3 edge0 = nv1 - nv0; 
		float3 vp0 = P - nv0; 
		C = cross(edge0, vp0); 
		if (dot(N, C) < 0) return false; // P is on the right side 
 
		// edge 1
		float3 edge1 = nv2 - nv1; 
		float3 vp1 = P - nv1; 
		C = cross(edge1, vp1); 
		if ((u = dot(N, C)) < 0)  return false; // P is on the right side 
 
		// edge 2
		float3 edge2 = nv0 - nv2; 
		float3 vp2 = P - nv2; 
		C = cross(edge2, vp2); 
		if ((v = dot(N, C)) < 0) return false; // P is on the right side; 
 
		u /= denom; 
		v /= denom; 
 
		return true; // t*///his ray hits the triangle 
	
	}
};


struct Mesh { 
	ptMaterial material;
	Triangle tris[16];
	//StructuredBuffer<Triangle> triangles;
	int numTris;
	float4x4 transform;

	bool intersect(inout Ray r, inout float t, inout float dd, inout int id, inout float cdepth, inout int isTriangle) {
		float d = 1e20;
		for (int j = int(12); j--;) { // test all scene objects for intersection
			//float dd = 0.0f;
			if ((d = tris[j].intersect(r, dd)) && d < t) {  // if newly computed intersection distance d is smaller than current closest intersection distance
				t = d;  // keep track of distance along ray to closest intersection point 
				id = j; // and closest intersected object
				cdepth = dd;			
				isTriangle = 1;
			}
		}
		return false;
	}
};

