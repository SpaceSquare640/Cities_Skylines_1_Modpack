import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const siteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const iconSource = process.env.MODPACK_ICON_SOURCE || path.resolve(siteRoot, '..', '..', '..', 'Resource', 'Icon', 'Other Design', 'Cities_Skylines_1_Modpack_Icon.png');
const iconOutput = path.join(siteRoot, 'public', 'icons');
const iconSizes = [
	['favicon-32.png', 32],
	['apple-touch-icon.png', 180],
	['icon-192.png', 192],
	['icon-512.png', 512],
];

await Promise.all(iconSizes.map(([name, size]) => sharp(iconSource)
	.resize(size, size, { fit: 'cover', position: 'centre' })
	.png({ compressionLevel: 9 })
	.toFile(path.join(iconOutput, name))));

await sharp(path.join(siteRoot, 'src', 'assets', 'social', 'social-preview.png'))
	.resize(1200, 630, { fit: 'cover', position: 'centre' })
	.webp({ quality: 78 })
	.toFile(path.join(siteRoot, 'public', 'social-preview.webp'));

console.log('Prepared 32/180/192/512 px app icons and optimized social preview.');
