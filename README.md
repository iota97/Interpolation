# Interpolation
Unity project to test interpolation of rototranslation expressed with different representation

## Supported types:
- Quaternion Slerp
- Quaternion Slerp with translation first
- Quaternion Slerp with translation first and origin in the middle of the two objects
- Quaternion Nlerp
- Axis (slerp) + angle (linear)
- Matrix
- Matrix converted back to rotation matrix
- Dual quaternion Slerp
- Dual quaternion Nlerp

Except for dual quaternion translation is lerped.