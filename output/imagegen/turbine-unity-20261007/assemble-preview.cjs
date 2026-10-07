// Encodes captured Unity frames without redrawing or simulating the artwork.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
(async () => {
  const dir = path.resolve(__dirname, '../../flight-validation');
  const names = fs.readdirSync(dir).filter(n => /^frame-\d{3}\.png$/.test(n)).sort();
  if (names.length !== 80) throw new Error('Expected 80 Unity capture frames');
  const frames = [];
  for (const name of names) frames.push(await sharp(path.join(dir, name)).removeAlpha().raw().toBuffer());
  const output = path.join(dir, 'flight-preview.gif');
  await sharp(Buffer.concat(frames), { raw: { width:640, height:640*frames.length, channels:3, pageHeight:640 } })
    .gif({ delay:Array(frames.length).fill(50), loop:0, effort:7, colours:192, dither:.25 }).toFile(output);
  console.log(output);
  console.log(await sharp(output, {animated:true}).metadata());
})().catch(error => { console.error(error); process.exit(1); });
