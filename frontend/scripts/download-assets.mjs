import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

const outDir = path.resolve("public/assets");
await mkdir(outDir, { recursive: true });

const assets = {
  "message.svg": "https://www.figma.com/api/mcp/asset/f30e0c9f-7ec7-4bdd-b679-cb1cda5170a1.svg",
  "folder.svg": "https://www.figma.com/api/mcp/asset/43075dc2-996b-46f5-a811-aea3c5d3b3c8.svg",
  "folder-mark.svg": "https://www.figma.com/api/mcp/asset/200274cb-6842-4684-ab1a-0ec9e8a50159.svg",
  "add-square-bulk.svg": "https://www.figma.com/api/mcp/asset/61486fa7-163b-4696-956d-ad46d00a4469.svg",
  "lamp.svg": "https://www.figma.com/api/mcp/asset/d85128ec-e85b-4f58-9067-b418512e8a41.svg",
  "link.svg": "https://www.figma.com/api/mcp/asset/58485dda-4327-45b1-a41e-9d5645a18456.svg",
  "edit.svg": "https://www.figma.com/api/mcp/asset/d425b486-ab35-4533-a65b-2b454b09d596.svg",
  "arrow-down.svg": "https://www.figma.com/api/mcp/asset/ab1621d7-ee1b-4b35-8524-1145de693d0c.svg",
  "filter.svg": "https://www.figma.com/api/mcp/asset/d666a756-df64-4867-8673-8fed9afe8fba.svg",
  "message-question.svg": "https://www.figma.com/api/mcp/asset/2266a291-954c-4d0a-8f51-6223caae598a.svg",
  "notification.svg": "https://www.figma.com/api/mcp/asset/43beae62-40bf-4e5c-81b2-02244fe55e21.svg",
  "notification-dot.svg": "https://www.figma.com/api/mcp/asset/ad317090-2665-4954-a6e1-32ba22b9a56e.svg",
  "arrow-down-lg.svg": "https://www.figma.com/api/mcp/asset/255fa922-d816-4632-b89f-20eb7272ab88.svg",
  "search.svg": "https://www.figma.com/api/mcp/asset/a8dfa898-c194-4241-b13f-18bb36699bad.svg",
  "logo.svg": "https://www.figma.com/api/mcp/asset/86da0e0b-9fc0-41ae-9225-d77a42adf346.svg",
  "add-square.svg": "https://www.figma.com/api/mcp/asset/597bb99b-aa51-4052-99f0-233212cff223.svg",
  "settings.svg": "https://www.figma.com/api/mcp/asset/fdcebb8a-b372-4e81-8c47-92288ffee89e.svg",
  "members.svg": "https://www.figma.com/api/mcp/asset/0575c6cf-3387-4ebb-9886-5cc52d7ee906.svg",
  "messages.svg": "https://www.figma.com/api/mcp/asset/80a485fb-c304-4a5d-89f9-4b3cba3862a3.svg",
  "tasks.svg": "https://www.figma.com/api/mcp/asset/135d144a-2985-45d5-9629-c7211797f5f4.svg",
  "home.svg": "https://www.figma.com/api/mcp/asset/a9b88758-4380-4c07-8195-462d0e400f8e.svg",
  "menu-top.svg": "https://www.figma.com/api/mcp/asset/6f815d2c-af76-4d6d-98b7-9a2a16c80b12.svg",
  "menu-bottom.svg": "https://www.figma.com/api/mcp/asset/51d5f56e-33ed-4842-ad70-7043d7a61089.svg",
  "menu.svg": "https://www.figma.com/api/mcp/asset/f5c8d360-0ccd-41e0-8c0f-8eefc03f9730.svg",
  "share.svg": "https://www.figma.com/api/mcp/asset/a02d9202-fa22-4db9-9bb7-68ea94cc696f.svg",
  "avatar-a.png": "https://www.figma.com/api/mcp/asset/136adecf-92dc-4fbf-a727-cf42504f5086.png",
  "avatar-b.png": "https://www.figma.com/api/mcp/asset/10b7c990-2d4a-4087-b1ae-ceb0c16ce3ef.png",
  "avatar-c.png": "https://www.figma.com/api/mcp/asset/5b641f4d-dc43-45e0-aa21-1df0605bb752.png",
  "avatar-d.png": "https://www.figma.com/api/mcp/asset/7a8d9192-e4dc-49c2-97af-c3533c67bda3.png",
  "avatar-e.png": "https://www.figma.com/api/mcp/asset/b8c36e57-f694-4873-9f29-04f142497af4.png",
  "photo-mood-1.png": "https://www.figma.com/api/mcp/asset/19102b3b-c1df-4df9-af3b-8576ceb3aed6.png",
  "photo-mood-2.png": "https://www.figma.com/api/mcp/asset/caf9fa86-d685-4865-9308-030705f86e60.png",
  "photo-onboarding.png": "https://www.figma.com/api/mcp/asset/7a3ccdb1-b0ea-4303-afce-324da2eb8dd6.png",
  "photo-mobile.png": "https://www.figma.com/api/mcp/asset/eff03d43-bf79-4cc3-80a6-36c58be44659.png",
  "avatar-f.png": "https://www.figma.com/api/mcp/asset/6ec43ddf-19bf-4c45-9818-56d1e906b082.png",
  "member-1.png": "https://www.figma.com/api/mcp/asset/6e4ffb25-ed8c-4c39-b195-82f49eef1288.png",
  "member-2.png": "https://www.figma.com/api/mcp/asset/a7114c86-9fcb-4c59-a5c9-024d6f316861.png",
  "member-3.png": "https://www.figma.com/api/mcp/asset/db05fba8-4df5-46a6-a453-ef693f286120.png",
  "member-4.png": "https://www.figma.com/api/mcp/asset/6f7e0f2e-f69a-4cdc-9509-a4dea88d3ce6.png",
  "avatar-g.png": "https://www.figma.com/api/mcp/asset/bb63f406-a585-43d6-8a3b-2f0ec7fc4956.png",
  "avatar-h.png": "https://www.figma.com/api/mcp/asset/0e593a73-b71c-4513-9461-280dc65fd676.png",
  "thoughts.svg": "https://www.figma.com/api/mcp/asset/c35cd5d5-5f65-4f41-8da9-e7a7cd319845.svg",
  "board-view.svg": "https://www.figma.com/api/mcp/asset/0a1dd9b4-9fef-4dd5-83dd-49ac65a2fbfc.svg",
  "divider-v.svg": "https://www.figma.com/api/mcp/asset/05041827-06f8-45b7-a341-51b72f5a729e.svg",
  "divider-sidebar.svg": "https://www.figma.com/api/mcp/asset/17395ba6-65f9-448c-9857-1c1b9e26e5b0.svg",
  "divider-header.svg": "https://www.figma.com/api/mcp/asset/e0d372b0-676f-4e15-b8a5-75058a232cfe.svg",
  "divider-projects.svg": "https://www.figma.com/api/mcp/asset/f4b174fb-c64b-4f37-9113-9fe79beaef5d.svg",
  "dot-green.svg": "https://www.figma.com/api/mcp/asset/9ce6aef9-d898-4d95-9fc2-2f8e88a9f909.svg",
  "dot-orange.svg": "https://www.figma.com/api/mcp/asset/58c90599-9aad-4afe-a369-6881a8347a99.svg",
  "dot-lilac.svg": "https://www.figma.com/api/mcp/asset/1bd9070f-b5fe-4536-a67a-10813cfaa0e8.svg",
  "dot-blue.svg": "https://www.figma.com/api/mcp/asset/ca84c88f-de33-4d50-aed0-775fb875e008.svg",
  "collapse.svg": "https://www.figma.com/api/mcp/asset/2b019804-953e-41b1-a254-985cab67fa1b.svg",
  "line-todo.svg": "https://www.figma.com/api/mcp/asset/5689b457-8452-4c39-b71d-b885544097f8.svg",
  "dot-todo.svg": "https://www.figma.com/api/mcp/asset/e3e9a352-ee9a-45c2-b184-3bf3ba40f44b.svg",
  "calendar.svg": "https://www.figma.com/api/mcp/asset/1095dbf2-559e-4425-a625-a0eec83b1231.svg",
  "calendar-sm.svg": "https://www.figma.com/api/mcp/asset/d14acfa5-6818-46be-b957-203f7e6dad42.svg",
  "link-box.svg": "https://www.figma.com/api/mcp/asset/7f087d15-a9dd-4715-ae65-8b9f23428df9.svg",
  "lamp-glow.svg": "https://www.figma.com/api/mcp/asset/82e3953f-9c74-49d5-9e49-b50d2069b28d.svg",
  "line-progress.svg": "https://www.figma.com/api/mcp/asset/787d3c7d-e47a-42b5-8275-eb5fe96abbf8.svg",
  "line-done.svg": "https://www.figma.com/api/mcp/asset/41e34693-cdfb-4d26-ac21-708e28fd5a1a.svg",
  "photo-mask.svg": "https://www.figma.com/api/mcp/asset/e3f94176-532b-44aa-a7c5-cf6725dec598.svg",
  "invite.svg": "https://www.figma.com/api/mcp/asset/5f5c265c-af4f-4a28-8337-1198ce9948f0.svg",
  "plus-count.svg": "https://www.figma.com/api/mcp/asset/1cc25670-abd4-420b-915f-8f9a38bd91ad.svg",
  "grab.svg": "https://www.figma.com/api/mcp/asset/cfd47472-efa6-40dc-9af1-66ce83c66c40.svg",
};

let failed = 0;
for (const [name, url] of Object.entries(assets)) {
  const res = await fetch(url);
  if (!res.ok) {
    console.error("FAIL", res.status, name);
    failed += 1;
    continue;
  }
  const buf = Buffer.from(await res.arrayBuffer());
  if (buf.length < 40) {
    console.error("EMPTY", name, buf.length);
    failed += 1;
    continue;
  }
  await writeFile(path.join(outDir, name), buf);
  console.log("OK", name, buf.length);
}
if (failed) {
  console.error(`Failed: ${failed}`);
  process.exit(1);
}
