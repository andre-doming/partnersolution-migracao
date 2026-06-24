from __future__ import annotations

from datetime import datetime

from sqlalchemy import Column, DateTime, Integer, String, Boolean, Float, create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

Base = declarative_base()


class ClientDB(Base):
    __tablename__ = "clients"

    id = Column(String, primary_key=True)
    firstName = Column(String, nullable=False)
    lastName = Column(String, nullable=False)
    email = Column(String, nullable=False)
    document = Column(String, nullable=False)
    companyId = Column(String, nullable=False)
    isActive = Column(Boolean, default=True)
    clientGuid = Column(String, nullable=False)
    syncedAtUtc = Column(DateTime, nullable=False)
    createdAt = Column(DateTime, default=datetime.utcnow)
    updatedAt = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class MetricDB(Base):
    __tablename__ = "metrics"

    id = Column(Integer, primary_key=True, autoincrement=True)
    totalRequests = Column(Integer, default=0)
    successCount = Column(Integer, default=0)
    errorCount = Column(Integer, default=0)
    timeoutCount = Column(Integer, default=0)
    averageResponseTimeMs = Column(Float, default=0.0)
    peakResponseTimeMs = Column(Float, default=0.0)
    requestsLastHour = Column(Integer, default=0)
    createdAt = Column(DateTime, default=datetime.utcnow)
    updatedAt = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


class ConfigDB(Base):
    __tablename__ = "config"

    id = Column(Integer, primary_key=True, autoincrement=True)
    failureRate = Column(Integer, default=1)
    forcedFailureMode = Column(String, default="none")
    latencyMs = Column(Integer, default=0)
    rateLimitPerMinute = Column(Integer, default=0)
    createdAt = Column(DateTime, default=datetime.utcnow)
    updatedAt = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)


def init_db(database_url: str):
    """Initialize database and create tables."""
    engine = create_engine(database_url, connect_args={"check_same_thread": False})
    Base.metadata.create_all(bind=engine)
    return engine


def get_session_factory(database_url: str):
    """Get session factory."""
    engine = init_db(database_url)
    return sessionmaker(autocommit=False, autoflush=False, bind=engine)
