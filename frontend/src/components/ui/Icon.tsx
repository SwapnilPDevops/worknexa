type IconProps = {
  src: string;
  width: number;
  height: number;
  alt?: string;
  className?: string;
};

export function Icon({ src, width, height, alt = "", className }: IconProps) {
  return <img src={src} width={width} height={height} alt={alt} className={className} />;
}

export function FolderIcon() {
  return (
    <span className="folder-icon">
      <Icon src="/assets/folder.svg" width={16} height={16} />
      <Icon src="/assets/folder-mark.svg" width={4.333} height={1} className="folder-mark" />
    </span>
  );
}

export function BellIcon() {
  return (
    <span className="bell-icon">
      <Icon src="/assets/notification.svg" width={24} height={24} alt="Notifications" />
      <Icon src="/assets/notification-dot.svg" width={6} height={6} className="bell-dot" />
    </span>
  );
}

export function GridIcon() {
  return (
    <span className="grid-icon" aria-hidden="true">
      <Icon src="/assets/menu-top.svg" width={16.195} height={6.5} className="grid-icon-row grid-icon-top" />
      <Icon src="/assets/menu-bottom.svg" width={16.195} height={6.5} className="grid-icon-row grid-icon-bottom" />
    </span>
  );
}

export function CollapseIcon() {
  return (
    <span className="collapse-icon" aria-hidden="true">
      <Icon src="/assets/collapse.svg" width={20} height={20} />
      <Icon src="/assets/collapse.svg" width={20} height={20} className="collapse-icon-second" />
    </span>
  );
}

export function LinkIcon() {
  return (
    <span className="link-icon">
      <Icon src="/assets/link-box.svg" width={30} height={30} />
      <Icon src="/assets/link.svg" width={16} height={16} className="link-icon-glyph" />
    </span>
  );
}
