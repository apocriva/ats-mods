//Import some assets from Vortex we'll need.
const path = require('path');
const { fs, log, util } = require('vortex-api');

const GAME_ID = 'againstthestorm';
const STEAMAPP_ID = '1336490';

function main(context) {
	context.registerGame({
		id: GAME_ID,
		name: 'Against the Storm',
		mergeMods: true,
		queryPath: findGame,
		supportedTools: [],
		queryModPath: () => 'BepInEx/plugins',
		logo: 'gameart.jpg',
		executable: () => 'Against the Storm.exe',
		requiredFiles: [
		  'Against the Storm.exe',
		],
		setup: prepareForModding,
		environment: {
		  SteamAPPId: STEAMAPP_ID,
		},
		details: {
		  steamAppId: STEAMAPP_ID,
		},
	});
	
	return true;
}

function findGame() {
	return util.GameStoreHelper.findByAppId([STEAMAPP_ID])
		.then(game => game.gamePath);
}

function prepareForModding(discovery) {
	return fs.ensureDirWritableAsync(path.join(discovery.path, 'BepInEx', 'plugins'));
}

module.exports = {
    default: main,
};