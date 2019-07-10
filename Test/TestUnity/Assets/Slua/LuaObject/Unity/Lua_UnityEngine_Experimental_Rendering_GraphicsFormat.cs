﻿using System;
using SLua;
using System.Collections.Generic;
[UnityEngine.Scripting.Preserve]
public class Lua_UnityEngine_Experimental_Rendering_GraphicsFormat : LuaObject {
	static public void reg(IntPtr l) {
		getEnumTable(l,"UnityEngine.Experimental.Rendering.GraphicsFormat");
		addMember(l,0,"None");
		addMember(l,1,"R8_SRGB");
		addMember(l,2,"R8G8_SRGB");
		addMember(l,3,"R8G8B8_SRGB");
		addMember(l,4,"R8G8B8A8_SRGB");
		addMember(l,5,"R8_UNorm");
		addMember(l,6,"R8G8_UNorm");
		addMember(l,7,"R8G8B8_UNorm");
		addMember(l,8,"R8G8B8A8_UNorm");
		addMember(l,9,"R8_SNorm");
		addMember(l,10,"R8G8_SNorm");
		addMember(l,11,"R8G8B8_SNorm");
		addMember(l,12,"R8G8B8A8_SNorm");
		addMember(l,13,"R8_UInt");
		addMember(l,14,"R8G8_UInt");
		addMember(l,15,"R8G8B8_UInt");
		addMember(l,16,"R8G8B8A8_UInt");
		addMember(l,17,"R8_SInt");
		addMember(l,18,"R8G8_SInt");
		addMember(l,19,"R8G8B8_SInt");
		addMember(l,20,"R8G8B8A8_SInt");
		addMember(l,21,"R16_UNorm");
		addMember(l,22,"R16G16_UNorm");
		addMember(l,23,"R16G16B16_UNorm");
		addMember(l,24,"R16G16B16A16_UNorm");
		addMember(l,25,"R16_SNorm");
		addMember(l,26,"R16G16_SNorm");
		addMember(l,27,"R16G16B16_SNorm");
		addMember(l,28,"R16G16B16A16_SNorm");
		addMember(l,29,"R16_UInt");
		addMember(l,30,"R16G16_UInt");
		addMember(l,31,"R16G16B16_UInt");
		addMember(l,32,"R16G16B16A16_UInt");
		addMember(l,33,"R16_SInt");
		addMember(l,34,"R16G16_SInt");
		addMember(l,35,"R16G16B16_SInt");
		addMember(l,36,"R16G16B16A16_SInt");
		addMember(l,37,"R32_UInt");
		addMember(l,38,"R32G32_UInt");
		addMember(l,39,"R32G32B32_UInt");
		addMember(l,40,"R32G32B32A32_UInt");
		addMember(l,41,"R32_SInt");
		addMember(l,42,"R32G32_SInt");
		addMember(l,43,"R32G32B32_SInt");
		addMember(l,44,"R32G32B32A32_SInt");
		addMember(l,45,"R16_SFloat");
		addMember(l,46,"R16G16_SFloat");
		addMember(l,47,"R16G16B16_SFloat");
		addMember(l,48,"R16G16B16A16_SFloat");
		addMember(l,49,"R32_SFloat");
		addMember(l,50,"R32G32_SFloat");
		addMember(l,51,"R32G32B32_SFloat");
		addMember(l,52,"R32G32B32A32_SFloat");
		addMember(l,56,"B8G8R8_SRGB");
		addMember(l,57,"B8G8R8A8_SRGB");
		addMember(l,58,"B8G8R8_UNorm");
		addMember(l,59,"B8G8R8A8_UNorm");
		addMember(l,60,"B8G8R8_SNorm");
		addMember(l,61,"B8G8R8A8_SNorm");
		addMember(l,62,"B8G8R8_UInt");
		addMember(l,63,"B8G8R8A8_UInt");
		addMember(l,64,"B8G8R8_SInt");
		addMember(l,65,"B8G8R8A8_SInt");
		addMember(l,66,"R4G4B4A4_UNormPack16");
		addMember(l,67,"B4G4R4A4_UNormPack16");
		addMember(l,68,"R5G6B5_UNormPack16");
		addMember(l,69,"B5G6R5_UNormPack16");
		addMember(l,70,"R5G5B5A1_UNormPack16");
		addMember(l,71,"B5G5R5A1_UNormPack16");
		addMember(l,72,"A1R5G5B5_UNormPack16");
		addMember(l,73,"E5B9G9R9_UFloatPack32");
		addMember(l,74,"B10G11R11_UFloatPack32");
		addMember(l,75,"A2B10G10R10_UNormPack32");
		addMember(l,76,"A2B10G10R10_UIntPack32");
		addMember(l,77,"A2B10G10R10_SIntPack32");
		addMember(l,78,"A2R10G10B10_UNormPack32");
		addMember(l,79,"A2R10G10B10_UIntPack32");
		addMember(l,80,"A2R10G10B10_SIntPack32");
		addMember(l,81,"A2R10G10B10_XRSRGBPack32");
		addMember(l,82,"A2R10G10B10_XRUNormPack32");
		addMember(l,83,"R10G10B10_XRSRGBPack32");
		addMember(l,84,"R10G10B10_XRUNormPack32");
		addMember(l,85,"A10R10G10B10_XRSRGBPack32");
		addMember(l,86,"A10R10G10B10_XRUNormPack32");
		addMember(l,90,"D16_UNorm");
		addMember(l,91,"D24_UNorm");
		addMember(l,92,"D24_UNorm_S8_UInt");
		addMember(l,93,"D32_SFloat");
		addMember(l,94,"D32_SFloat_S8_Uint");
		addMember(l,95,"S8_Uint");
		addMember(l,96,"RGB_DXT1_SRGB");
		addMember(l,96,"RGBA_DXT1_SRGB");
		addMember(l,97,"RGB_DXT1_UNorm");
		addMember(l,97,"RGBA_DXT1_UNorm");
		addMember(l,98,"RGBA_DXT3_SRGB");
		addMember(l,99,"RGBA_DXT3_UNorm");
		addMember(l,100,"RGBA_DXT5_SRGB");
		addMember(l,101,"RGBA_DXT5_UNorm");
		addMember(l,102,"R_BC4_UNorm");
		addMember(l,103,"R_BC4_SNorm");
		addMember(l,104,"RG_BC5_UNorm");
		addMember(l,105,"RG_BC5_SNorm");
		addMember(l,106,"RGB_BC6H_UFloat");
		addMember(l,107,"RGB_BC6H_SFloat");
		addMember(l,108,"RGBA_BC7_SRGB");
		addMember(l,109,"RGBA_BC7_UNorm");
		addMember(l,110,"RGB_PVRTC_2Bpp_SRGB");
		addMember(l,111,"RGB_PVRTC_2Bpp_UNorm");
		addMember(l,112,"RGB_PVRTC_4Bpp_SRGB");
		addMember(l,113,"RGB_PVRTC_4Bpp_UNorm");
		addMember(l,114,"RGBA_PVRTC_2Bpp_SRGB");
		addMember(l,115,"RGBA_PVRTC_2Bpp_UNorm");
		addMember(l,116,"RGBA_PVRTC_4Bpp_SRGB");
		addMember(l,117,"RGBA_PVRTC_4Bpp_UNorm");
		addMember(l,118,"RGB_ETC_UNorm");
		addMember(l,119,"RGB_ETC2_SRGB");
		addMember(l,120,"RGB_ETC2_UNorm");
		addMember(l,121,"RGB_A1_ETC2_SRGB");
		addMember(l,122,"RGB_A1_ETC2_UNorm");
		addMember(l,123,"RGBA_ETC2_SRGB");
		addMember(l,124,"RGBA_ETC2_UNorm");
		addMember(l,125,"R_EAC_UNorm");
		addMember(l,126,"R_EAC_SNorm");
		addMember(l,127,"RG_EAC_UNorm");
		addMember(l,128,"RG_EAC_SNorm");
		addMember(l,129,"RGBA_ASTC4X4_SRGB");
		addMember(l,130,"RGBA_ASTC4X4_UNorm");
		addMember(l,131,"RGBA_ASTC5X5_SRGB");
		addMember(l,132,"RGBA_ASTC5X5_UNorm");
		addMember(l,133,"RGBA_ASTC6X6_SRGB");
		addMember(l,134,"RGBA_ASTC6X6_UNorm");
		addMember(l,135,"RGBA_ASTC8X8_SRGB");
		addMember(l,136,"RGBA_ASTC8X8_UNorm");
		addMember(l,137,"RGBA_ASTC10X10_SRGB");
		addMember(l,138,"RGBA_ASTC10X10_UNorm");
		addMember(l,139,"RGBA_ASTC12X12_SRGB");
		addMember(l,140,"RGBA_ASTC12X12_UNorm");
		LuaDLL.lua_pop(l, 1);
	}
}
