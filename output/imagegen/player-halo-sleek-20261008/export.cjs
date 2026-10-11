// Export AI-generated artwork; no procedural replacement of ship designs.
const fs=require('fs'),path=require('path'),sharp=require('sharp');
const root=__dirname;
(async()=>{
const allJobs=JSON.parse(fs.readFileSync(path.join(root,'prompts.json'),'utf8')).jobs;
const manifest=JSON.parse(fs.readFileSync(path.join(root,'manifest.json'),'utf8'));
const jobs=allJobs.filter(j=>manifest.some(m=>m.id===j.id));
for(const folder of ['originals','sprites48','previews'])fs.mkdirSync(path.join(root,folder),{recursive:true});
const validations=[], tiles=[];
const tones=['연보라 · 청회색'];
for(let i=0;i<jobs.length;i++){
 const j=jobs[i],source=manifest.find(x=>x.id===j.id).path;
 fs.copyFileSync(source,path.join(root,'originals',j.id+'.png'));
 const {data,info}=await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let l=info.width,t=info.height,r=-1,b=-1,transparent=0;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
  let a=data[(y*info.width+x)*4+3];
  if(a<64)transparent++;
  if(a>=128){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}
 }
 if(!transparent||r<l)throw Error(j.id+': needs real transparency');
 const bw=r-l+1,bh=b-t+1,s=44/Math.max(bw,bh),w=Math.max(1,Math.round(bw*s)),h=Math.max(1,Math.round(bh*s));
 let pixels=await sharp(source).extract({left:l,top:t,width:bw,height:bh}).resize(w,h,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=3;p<pixels.length;p+=4)pixels[p]=pixels[p]>=128?255:0;
 const inset=await sharp(pixels,{raw:{width:w,height:h,channels:4}}).png().toBuffer();
 const png=await sharp({create:{width:48,height:48,channels:4,background:'#00000000'}}).composite([{input:inset,left:Math.floor((48-w)/2),top:Math.floor((48-h)/2)}]).png({palette:true,colours:12,dither:0}).toBuffer();
 const file=path.join(root,'sprites48',j.id+'.png');fs.writeFileSync(file,png);
 await sharp(png).resize(384,384,{kernel:'nearest'}).png().toFile(path.join(root,'previews',j.id+'-8x.png'));
 const big=await sharp(png).resize(288,288,{kernel:'nearest'}).toBuffer();
 const svg=Buffer.from('<svg width="336" height="414" xmlns="http://www.w3.org/2000/svg"><rect width="336" height="414" fill="#202734"/><rect x="16" y="53" width="304" height="304" rx="4" fill="#5b639d"/><text x="20" y="34" font-family="Malgun Gothic" font-size="20" fill="#eee4cf">'+String(i+1).padStart(2,'0')+' '+j.name+'</text><text x="20" y="389" font-family="Malgun Gothic" font-size="14" fill="#b8c3d3">'+tones[i]+' · 48×48 / 6×</text></svg>');
 const tile=await sharp(svg).composite([{input:big,left:24,top:61}]).png().toBuffer();
 tiles.push({input:tile,left:i*336,top:0});
 const raw=await sharp(png).ensureAlpha().raw().toBuffer();let solid=0;for(let p=3;p<raw.length;p+=4)if(raw[p])solid++;
 validations.push({id:j.id,width:48,height:48,sourceSize:[info.width,info.height],opaquePixels:solid,transparent:true,paletteMax:12,previewScale:8});
}
await sharp({create:{width:336,height:414,channels:4,background:'#202734'}}).composite(tiles).png().toFile(path.join(root,'overview.png'));
fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify({generatedWith:'built-in image_gen',export:'nearest-neighbor, binary alpha, 12-color maximum',assets:validations},null,2));
console.log(JSON.stringify(validations,null,2));
})().catch(e=>{console.error(e);process.exit(1)});




