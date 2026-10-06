import { useEffect, useRef, useState } from "react";
import { members } from "../data/board";
import { AvatarGroup } from "../components/ui/AvatarGroup";
import { BellIcon, Icon } from "../components/ui/Icon";

type HeaderProps = {
  query: string;
  onQuery: (value: string) => void;
};

export function Header({ query, onQuery }: HeaderProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!menuOpen) return;
    const close = (event: MouseEvent) => {
      if (!profileRef.current?.contains(event.target as Node)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [menuOpen]);

  return (
    <header className="topbar">
      <form className="search" role="search" onSubmit={(event) => event.preventDefault()}>
        <Icon src="/assets/search.svg" width={22} height={22} alt="" />
        <input
          type="search"
          value={query}
          onChange={(event) => onQuery(event.target.value)}
          placeholder="Search for anything..."
          aria-label="Search for anything"
        />
      </form>
      <div className="header-tools">
        <div className="header-icons">
          <button type="button" className="icon-hit" aria-label="Calendar">
            <Icon src="/assets/calendar.svg" width={24} height={24} alt="" />
          </button>
          <button type="button" className="icon-hit" aria-label="Help">
            <Icon src="/assets/message-question.svg" width={24} height={24} alt="" />
          </button>
          <button type="button" className="icon-hit" aria-label="Notifications">
            <BellIcon />
          </button>
        </div>
        <div className="profile" ref={profileRef}>
          <button type="button" className="profile-button" aria-expanded={menuOpen} onClick={() => setMenuOpen((open) => !open)}>
            <span className="profile-text">
              <span className="profile-name">Anima Agrawal</span>
              <span className="profile-place">U.P, India</span>
            </span>
            <Icon src="/assets/arrow-down-lg.svg" width={18} height={18} alt="" />
          </button>
          {menuOpen ? (
            <div className="profile-menu" role="menu">
              <p className="profile-menu-name">Anima Agrawal</p>
              <p className="profile-menu-place">U.P, India</p>
              <div className="profile-menu-people">
                <AvatarGroup sources={members} size={32} overlap={8} />
              </div>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  );
}
