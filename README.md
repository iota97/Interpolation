# Interpolation
Unity project to test interpolation of rototranslation expressed with different representation

## Supported types:
- Quaternion Slerp
- Quaternion Nlerp
- Axis (slerp) + angle (linear)
- Matrix (approximation as Unity don't make it easy to set the Model matrix)
- Dual quaternion

Except for dual quaternion translation is lerped.