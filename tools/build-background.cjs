/* Assemble the supplied tile pixels; no painted/generated replacement art. */
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');
const sharp = require('sharp');

const root = path.resolve(__dirname, '..');
const tileDir = path.join(root, 'IMG', '배경');
const qaDir = path.join(root, '.work', 'background-20261005');
const filename = '연결배경_1920x1080.png';
const width = 1920;
const height = 1080;
const scale = 3;
const logicalWidth = width / scale;
const logicalHeight = height / scale;
// Six complete 180px repeat bands fit 1080px. Resample 64 -> 60 source rows
// with nearest-neighbour selection, then use a crisp 3x3 display-pixel grid.
const repeatRows = 60;
const leftCliff = 96;
const rightCliff = 480;
const tileSize = 64;

async function readTile(name) {
  const {data, info} = await sharp(path.join(tileDir, name))
    .ensureAlpha().raw().toBuffer({resolveWithObject: true});
  assert.equal(info.width, tileSize);
  assert.equal(info.height, tileSize);
  assert.equal(info.channels, 4);
  for (let p = 3; p < data.length; p += 4) assert.equal(data[p], 255);
  return data;
}

function pixelAt(tiles, x, y) {
  const phase = ((y % repeatRows) + repeatRows) % repeatRows;
  const sy = Math.floor((phase + 0.5) * tileSize / repeatRows);
  let tile;
  let sx;
  if (x < leftCliff) {
    tile = tiles.R;
    // Match the left cliff's mirrored light-ground edge.
    sx = tileSize - 1 - (x % tileSize);
  } else if (x < leftCliff + tileSize) {
    tile = tiles.M;
    sx = tileSize - 1 - (x - leftCliff);
  } else if (x < rightCliff) {
    tile = tiles.L;
    sx = (x - leftCliff - tileSize) % tileSize;
  } else if (x < rightCliff + tileSize) {
    tile = tiles.M;
    sx = x - rightCliff;
  } else {
    tile = tiles.R;
    sx = (x - rightCliff - tileSize) % tileSize;
  }
  return tile.subarray((sy * tileSize + sx) * 4, (sy * tileSize + sx) * 4 + 4);
}

async function main() {
  await fs.mkdir(qaDir, {recursive: true});
  const [L, M, R] = await Promise.all(['01_L.png', '01_M.png', '01_R.png'].map(readTile));
  const tiles = {L, M, R};
  const logical = Buffer.alloc(logicalWidth * logicalHeight * 4);
  for (let y = 0; y < logicalHeight; y++) {
    for (let x = 0; x < logicalWidth; x++) {
      pixelAt(tiles, x, y).copy(logical, (y * logicalWidth + x) * 4);
    }
  }
  const final = await sharp(logical, {
    raw: {width: logicalWidth, height: logicalHeight, channels: 4},
  }).resize(width, height, {kernel: 'nearest'}).png().toBuffer();
  await fs.writeFile(path.join(tileDir, filename), final);

  const {data: decoded, info} = await sharp(final).raw().toBuffer({resolveWithObject: true});
  assert.equal(info.width, width);
  assert.equal(info.height, height);
  assert.equal(info.channels, 4);
  // Every row is an exact tile sample, including the frame-to-frame wrap.
  for (let y = 0; y < height + 180; y++) {
    const actualY = y % height;
    for (let x = 0; x < width; x++) {
      const expected = pixelAt(tiles, Math.floor(x / scale), Math.floor(y / scale));
      const index = (actualY * width + x) * 4;
      for (let c = 0; c < 4; c++) assert.equal(decoded[index + c], expected[c]);
    }
  }

  // Inspect the join with the last 180px immediately above the first 180px.
  const end = await sharp(final).extract({left: 0, top: height - 180, width, height: 180}).toBuffer();
  const start = await sharp(final).extract({left: 0, top: 0, width, height: 180}).toBuffer();
  await sharp({create: {width, height: 360, channels: 4, background: '#000'}})
    .composite([{input: end, top: 0, left: 0}, {input: start, top: 180, left: 0}])
    .png().toFile(path.join(qaDir, 'vertical-wrap.png'));

  // Animated half-resolution preview, one exact 180px downward loop.
  const previewWidth = 960;
  const previewHeight = 540;
  const frames = 30;
  const small = await sharp(final).resize(previewWidth, previewHeight, {kernel: 'nearest'})
    .removeAlpha().raw().toBuffer();
  const stride = previewWidth * 3;
  const pages = Buffer.alloc(small.length * frames);
  for (let frame = 0; frame < frames; frame++) {
    const offset = frame * 3;
    for (let y = 0; y < previewHeight; y++) {
      const sy = (y - offset + previewHeight) % previewHeight;
      small.copy(pages, (frame * previewHeight + y) * stride, sy * stride, (sy + 1) * stride);
    }
  }
  const previewPath = path.join(tileDir, '연결배경_스크롤미리보기.gif');
  await sharp(pages, {raw: {
    width: previewWidth, height: previewHeight * frames, channels: 3, pageHeight: previewHeight,
  }}).gif({loop: 0, delay: 60, effort: 5, dither: 0}).toFile(previewPath);
  const previewInfo = await sharp(previewPath, {animated: true}).metadata();
  assert.equal(previewInfo.pages, frames);
  assert.equal(previewInfo.pageHeight, previewHeight);

  const report = {
    output: path.join(tileDir, filename),
    size: [width, height],
    opaque: true,
    interpolation: 'nearest-neighbour only; exact source palette',
    pixelGrid: '3x3',
    tileDisplaySize: [192, 180],
    verticalPeriod: 180,
    verticalRepeats: 6,
    layout: [
      {kind: 'R mirrored', x: 0, width: 288},
      {kind: 'M mirrored', x: 288, width: 192},
      {kind: 'L repeated', x: 480, width: 960},
      {kind: 'M', x: 1440, width: 192},
      {kind: 'R repeated', x: 1632, width: 288},
    ],
    validation: 'PASS: 1920x1080, opaque, source pixels only, six complete periods, exact vertical wrap',
    preview: {file: previewPath, size: [960, 540], frames, direction: 'down'},
  };
  await fs.writeFile(path.join(qaDir, 'validation.json'), JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report, null, 2));
}

main().catch(error => {console.error(error); process.exitCode = 1;});
