type AvatarGroupProps = {
  sources: string[];
  size: number;
  overlap?: number;
};

export function AvatarGroup({ sources, size, overlap = 8 }: AvatarGroupProps) {
  return (
    <span className="avatars" style={{ ["--avatar-size" as string]: `${size}px`, ["--avatar-overlap" as string]: `${overlap}px` }}>
      {sources.map((src) => (
        <img key={src} src={src} width={size} height={size} alt="" className="avatar" />
      ))}
    </span>
  );
}
