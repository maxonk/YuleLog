float4 frustumNear[4], frustumFar[4];

const int bottomLeft = 0,
 bottomRight = 3,
 topLeft = 1,
 topRight = 2;

float4 worldToNormalizedFrustumSpace(float4 worldPos){
	return worldPos;
}

float4 normalizedFrustumToWorldSpace(float4 normalizedFrustumPos){
	return normalizedFrustumPos;
}