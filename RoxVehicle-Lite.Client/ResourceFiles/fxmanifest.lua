fx_version 'cerulean'
game 'gta5'
author 'Roxstar Studios'
description 'RoxVehicle-Lite was created by Roxstar Studios'
version '1.0.1'

client_script 'Client/RoxVehicle-Lite.Client.net.dll'

files {	
    'Client/*.dll',
    '*.jsonc',
	'dlc_defaultantilag/AntiLagDefault.awc',
	'data/defaultantilag.dat54.rel',
	'data/defaultantilag.dat54.nametable',
}   

data_file "AUDIO_WAVEPACK" "dlc_defaultantilag"
data_file "AUDIO_SOUNDDATA" "data/defaultantilag.dat"